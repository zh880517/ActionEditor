# StructSequence：非托管结构体队列技术方案

## Context

设计一套独立、可复用的非托管内存结构体队列系统。生产方将数据填充完整块，消费方按序处理。支持引用类型字段，代码生成驱动序列化，读写使用指针 + 偏移直接操作。

---

## 1. 设计目标

| 目标 | 说明 |
|------|------|
| 单线程批量生产/消费 | 无锁，生产者填满一批 → 消费者一次性处理 → Reset |
| 支持引用类型字段 | struct 字段可以是 string、数组、class 等 |
| 指针 + 偏移读写 | 生成代码计算每个字段的固定偏移，用 unsafe 指针直接读写 |
| 链表式固定分块 | 内存以固定大小块（InternalSequence）组织，写满后链到下一块，块可回收复用 |
| 代码生成驱动 | 用户只定义 struct，读写和分发由代码生成（本次设计接口，不实现生成器） |

---

## 2. 核心组件

### 2.1 IUnmanagedStruct（标记接口）

```csharp
public interface IUnmanagedStruct { }
```

纯标记，无方法。代码生成器扫描所有实现此接口的 struct。

### 2.2 InternalSequence（固定大小内存块）

每个块是一段固定大小的非托管内存 + 配套的托管引用列表。多个块通过 `next` 指针组成单链表。

**字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `_memory` | `byte*` (IntPtr) | 非托管内存块，`Marshal.AllocHGlobal` 分配 |
| `_writeOffset` | `int` | 当前写偏移 |
| `_readOffset` | `int` | 当前读偏移（Consume 时使用） |
| `_capacity` | `int` | 块容量（固定值，硬编码默认 4KB） |
| `_messageCount` | `int` | 本块内已写入消息数 |
| `_references` | `List<object>` | 本块的引用对象存储 |
| `next` | `InternalSequence` | 链表下一个块 |

**关键方法**：

| 方法 | 说明 |
|------|------|
| `byte* TryAlloc(int size)` | 剩余空间 >= size 时返回写指针并前进偏移；不足时返回 null |
| `byte* AllocRead(int size)` | 返回读指针并前进读偏移 |
| `int WriteRef(object obj)` | 追加到 `_references`，null 返回 -1 |
| `object GetRef(int index)` | index < 0 返回 null，否则返回 `_references[index]` |
| `void Reset()` | 读写偏移归零，`_references.Clear()`，`_messageCount = 0`，`next = null` |

**消息不跨块**：当 `TryAlloc` 返回 null（空间不足），StructSequence 将当前块链到下一块，在新块上重新 `TryAlloc`。块尾部可能有少量浪费空间。

### 2.3 InternalSequence 对象池

避免反复 `new InternalSequence()` 和 `Marshal.AllocHGlobal/FreeHGlobal`，用一个简单的静态或实例级栈池管理空闲块。

| 方法 | 说明 |
|------|------|
| `InternalSequence Rent()` | 从池中取一个空闲块，池空时新建 |
| `void Return(InternalSequence block)` | Reset 块后归还池中 |

Reset 时 StructSequence 遍历整条链表，逐块 Return。

### 2.4 StructSequence（队列主体）

**字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `_head` | `InternalSequence` | 链表头（消费从这里开始） |
| `_current` | `InternalSequence` | 当前写入块 |
| `_handlers` | `Action<InternalSequence>[]` | 下标 = typeIndex，handler 从块中读取 payload |
| `_totalMessageCount` | `int` | 所有块的消息总数 |
| `_pool` | 对象池 | InternalSequence 块的回收池 |

**关键方法**：

| 方法 | 说明 |
|------|------|
| `void Push<T>(T data)` | 调用生成的 Write 方法；若当前块 TryAlloc 失败，分配新块接到链表尾部后重试 |
| `void Consume()` | 从 head 开始遍历链表，每块内逐条读 header → 查 handler → 调用 |
| `void Reset()` | 遍历链表，所有块归还对象池；head/current 重新 Rent 一个空块 |
| `void Init()` | 初始化，Rent 第一个块，head = current = 该块 |
| `void Dispose()` | 遍历链表释放所有块的非托管内存 |

---

## 3. 内存布局

### 单个 InternalSequence 块内

```
┌─────────────────────────────────────────────────┐
│ [Msg0: hdr+payload] [Msg1: hdr+payload] [waste] │
│                                          ↑      │
│                                   _writeOffset   │
└─────────────────────────────────────────────────┘
capacity = 4096 bytes (固定)

配套托管侧:
List<object> { ref0, ref1, ref2, ... }  // 本块内消息的引用
```

### 多块链表

```
head                          current
 ↓                              ↓
[Block0: 已满] → [Block1: 已满] → [Block2: 写入中] → null
```

### 单条消息

```
[typeIndex: int, 4B] [field0] [field1] ... [fieldN]
```

- 值类型字段：写值本身，占 sizeof(T)
- 引用类型字段：写 int 索引 (4B)，对象在本块的 `_references` 中

---

## 4. 代码生成策略

### 4.1 类型索引

```csharp
public static class StructTypeIndex
{
    public const int DamageEvent = 0;
    public const int MoveEvent = 1;
    public const int Max = N;
}
```

### 4.2 Payload 大小常量

生成器编译期计算每种消息的固定大小：

```
PayloadSize(T) = Sum(
    值类型字段 → sizeof(字段类型),
    引用类型字段 → 4,
    含引用字段的嵌套 struct → 递归累加
)
MessageSize(T) = 4 (header) + PayloadSize(T)
```

### 4.3 Write 方法（指针 + 偏移写入）

偏移量是编译期常量。生成代码对每个字段计算好偏移后直接指针操作：

```csharp
// 用户定义
public struct DamageEvent : IUnmanagedStruct
{
    public int targetId;       // 偏移 0, 4B
    public double damage;      // 偏移 4, 8B
    public string skillName;   // 偏移 12, 4B (引用索引)
}
// MessageSize = 4 + 4 + 8 + 4 = 20

// 生成代码
static unsafe void Write_DamageEvent(ref DamageEvent data, InternalSequence block)
{
    byte* ptr = block.TryAlloc(20);  // 调用方已确保空间足够
    *(int*)(ptr + 0) = StructTypeIndex.DamageEvent;  // header
    *(int*)(ptr + 4) = data.targetId;
    *(double*)(ptr + 8) = data.damage;
    *(int*)(ptr + 16) = block.WriteRef(data.skillName);
    block._messageCount++;
}
```

### 4.4 Read 方法（指针 + 偏移读取）

```csharp
// 生成代码
static unsafe DamageEvent Read_DamageEvent(InternalSequence block)
{
    byte* ptr = block.AllocRead(16);  // payload size，header 已被 Consume 读过
    DamageEvent data;
    data.targetId = *(int*)(ptr + 0);
    data.damage = *(double*)(ptr + 4);
    data.skillName = (string)block.GetRef(*(int*)(ptr + 12));
    return data;
}
```

### 4.5 Handler 包装

```csharp
handlers[StructTypeIndex.DamageEvent] = (block) =>
{
    var data = Read_DamageEvent(block);
    userHandler_DamageEvent(data);
};
```

### 4.6 偏移计算规则

```
offset = 0
for each field in struct (按声明顺序):
    if 值类型:
        emit: *(FieldType*)(ptr + offset) = data.field
        offset += sizeof(FieldType)
    if 引用类型:
        emit: *(int*)(ptr + offset) = block.WriteRef(data.field)
        offset += 4
    if 含引用字段的嵌套 struct:
        递归展开各字段，offset 继续累加
PayloadSize = offset
```

---

## 5. Consume 流程

```csharp
public unsafe void Consume()
{
    var block = _head;
    while (block != null)
    {
        block._readOffset = 0;
        for (int i = 0; i < block._messageCount; i++)
        {
            byte* hdr = block.AllocRead(4);
            int typeIndex = *(int*)hdr;
            _handlers[typeIndex](block);  // handler 内调用 AllocRead 读 payload
        }
        block = block.next;
    }
}
```

### Reset 流程

```csharp
public void Reset()
{
    var block = _head;
    while (block != null)
    {
        var next = block.next;
        block.Reset();      // 偏移归零、引用清空、next = null
        _pool.Return(block); // 归还池
        block = next;
    }
    _head = _pool.Rent();
    _current = _head;
    _totalMessageCount = 0;
}
```

---

## 6. Push 流程（StructSequence 层）

```csharp
public void Push<T>(T data) where T : IUnmanagedStruct
{
    // 生成代码知道 MessageSize(T) 是编译期常量
    if (_current.Remaining < MessageSize_T)
    {
        var newBlock = _pool.Rent();
        _current.next = newBlock;
        _current = newBlock;
    }
    Write_T(ref data, _current);  // 生成的写入方法
    _totalMessageCount++;
}
```

**注意**：如果单条消息的 MessageSize > 块容量，则为设计错误，应在初始化或生成期检查报错。

---

## 7. 引用生命周期

单线程下 Push → Consume → Reset 严格顺序执行：

1. **Push 时**：引用追加到当前块的 `List<object>`，非托管内存写入索引
2. **Consume 时**：handler 从块的列表取出引用，组装 struct 传给用户回调。用户持有引用后 GC 根集已有该对象
3. **Reset 时**：`_references.Clear()` 安全——消费者已持有所需引用

每个块有独立的 `_references` 列表，引用索引是块内局部的（从 0 开始），不同块的索引互不影响。

---

## 8. 使用流程

```csharp
// 1. 定义消息
public struct HeroDamageEvent : IUnmanagedStruct
{
    public int attackerId;
    public int targetId;
    public double damage;
    public string skillName;  // 引用字段
}

// 2. 初始化
var seq = new StructSequence();
seq.Init();

// 3. 注册 handler
seq.Register<HeroDamageEvent>(data =>
{
    ShowDamage(data.targetId, data.damage, data.skillName);
});

// 4. 帧循环
while (running)
{
    LogicUpdate();     // 内部调用 seq.Push(new HeroDamageEvent { ... })
    seq.Consume();     // 从 head 遍历链表，逐块逐条处理
    seq.Reset();       // 所有块归还池，重新 Rent 一个空块
}
```

---

## 9. 关键设计决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 内存组织 | 固定大小块链表 + 对象池 | 避免单块扩容时的 MemoryCopy 和重分配开销；块可复用 |
| 块大小 | 硬编码默认值（如 4KB） | 简化接口，对大多数消息场景足够 |
| 跨块消息 | 不允许 | 简化读写逻辑，消息一定完整落在一个块内；尾部少量空间浪费可接受 |
| 读写方式 | 指针 + 偏移 | 偏移量是编译期常量，无序列化开销 |
| 引用存储 | 每块独立 `List<object>` | 索引是块内局部的，Reset 时各块独立清理 |
| 引用索引空间 | 块内局部，从 0 开始 | 块互不影响，无全局索引管理 |
| null 引用 | WriteRef(null) 返回 -1，不入列表 | 节省列表空间 |
| 对象池 | InternalSequence 级别 | 非托管内存 + List 一起复用，避免 AllocHGlobal/FreeHGlobal |
| Header | 仅 int typeIndex (4B) | 无需消费标记、引用计数（单线程整批消费） |

---

## 10. 实现清单

手写核心代码：

1. **`IUnmanagedStruct.cs`** — 标记接口
2. **`InternalSequence.cs`** — 固定大小内存块：TryAlloc / AllocRead / WriteRef / GetRef / Reset / Dispose + next 链表指针
3. **`InternalSequencePool.cs`** — 块对象池：Rent / Return
4. **`StructSequence.cs`** — 队列主体：Push / Consume / Reset / Register / Init / Dispose

手写验证示例（模拟代码生成器产出）：

5. **测试 struct** — 含 int、double、string、数组等字段
6. **手写 Write/Read** — 用指针 + 偏移实现
7. **测试用例** — 多条消息 Push → Consume → 验证数据和引用正确

---

## 11. 验证方式

1. 定义含值类型 + 引用类型字段的测试 struct
2. 手写 Write/Read 方法（模拟生成器产出）
3. Push 多条不同类型消息 → Consume → 验证 handler 收到正确数据
4. Push 足够多消息触发多块链表 → 验证跨块消费正确
5. 验证引用字段值正确，Reset 后消费者仍持有引用
6. 验证 null 引用字段正确处理
7. 验证 Reset 后块归还池，下次 Push 复用块
