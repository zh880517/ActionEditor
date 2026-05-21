# StructSequence

单线程非托管内存结构体队列。生产者按批次写入消息，消费者通过 Meta 列表按序读取，读完后 Reset 复用内存块。

支持纯值类型 struct（自动处理）和含引用字段的 struct（通过代码生成注册委托）。

---

## 核心概念

- **IStructSequenceWriter** — 写入接口，生产者持有，只能 Push
- **IStructSequenceReader** — 读取接口，消费者持有，只能读 Metas / Read
- **StructSequence** — 同时实现两个接口，管理内存块链表和 Meta 列表
- **SequenceMeta** — 记录每条消息的 `(MessageID, Block, Offset)`，消费时通过它定位 payload
- **MessageID** — 调用方自定义的 int 常量，同一个 struct 可以用不同 MessageID 表达不同语义
- **UnmanagedStructReadWrite\<T\>** — 每种 struct 的读写实现，纯值类型自动使用 Marshal 拷贝，含引用字段的 struct 需调用 `Init` 注册自定义委托

---

## 快速上手

### 1. 定义消息 struct

```csharp
public struct MoveEvent : IUnmanagedStruct
{
    public int entityId;
    public float x, y, z;
}

public struct DamageEvent : IUnmanagedStruct
{
    public int targetId;
    public double damage;
    public string skillName;  // 引用字段，需注册委托
}
```

### 2. 注册含引用字段的 struct（启动时执行一次）

纯值类型（如 `MoveEvent`）无需注册，跳过此步。

```csharp
unsafe
{
    UnmanagedStructReadWrite<DamageEvent>.Init(
        size: 16,  // int(4) + double(8) + ref index(4)
        writeFunc: (InternalSequence block, byte* ptr, ref DamageEvent v) =>
        {
            *(int*)(ptr + 0)    = v.targetId;
            *(double*)(ptr + 4) = v.damage;
            *(int*)(ptr + 12)   = block.WriteRef(v.skillName);
        },
        readFunc: (InternalSequence block, byte* ptr) =>
        {
            DamageEvent data;
            data.targetId  = *(int*)(ptr + 0);
            data.damage    = *(double*)(ptr + 4);
            data.skillName = (string)block.GetRef(*(int*)(ptr + 12));
            return data;
        }
    );
}
```

引用字段的 payload 大小计算规则：值类型字段用 `sizeof`，引用类型字段固定占 4 字节（存引用索引）。

### 3. 初始化

```csharp
var seq = new StructSequence();
seq.Init();

IStructSequenceWriter writer = seq;
IStructSequenceReader reader = seq;
```

### 4. 写入消息（生产者）

```csharp
var move = new MoveEvent { entityId = 1, x = 1f, y = 0f, z = 0f };
writer.Push(MessageIDs.Move, ref move);

var dmg = new DamageEvent { targetId = 2, damage = 50.0, skillName = "Fireball" };
writer.Push(MessageIDs.Damage, ref dmg);
```

同一 struct 可用不同 MessageID 表达不同语义：

```csharp
writer.Push(MessageIDs.DamageNormal, ref dmg);
writer.Push(MessageIDs.DamageCrit,   ref dmg);
```

### 5. 读取消息（消费者）

```csharp
var metas = reader.Metas;
for (int i = 0; i < metas.Count; i++)
{
    var meta = metas[i];
    switch (meta.MessageID)
    {
        case MessageIDs.Move:
            var move = reader.Read<MoveEvent>(meta);
            HandleMove(move);
            break;
        case MessageIDs.Damage:
        case MessageIDs.DamageCrit:
            var dmg = reader.Read<DamageEvent>(meta);
            HandleDamage(meta.MessageID, dmg);
            break;
    }
}
```

### 6. Reset（帧结束后）

```csharp
seq.Reset();  // 清空 Metas，内存块归还对象池复用
```

Reset 前消费者持有的引用仍然有效；Reset 后不应再访问任何 meta 或引用。

### 7. 销毁

```csharp
seq.Dispose();
```

---

## 典型帧循环

```csharp
// 每帧
LogicUpdate(writer);   // 内部调用 writer.Push(...)
RenderUpdate(reader);  // 内部遍历 reader.Metas
seq.Reset();
```
