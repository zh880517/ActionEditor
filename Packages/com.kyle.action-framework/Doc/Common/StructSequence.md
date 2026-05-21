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

### 1. 定义分类 Attribute 并标记 struct

创建一个继承 `StructSequenceCatalogAttribute` 的 Attribute 子类，定义命名空间和生成路径；然后用它标记所有属于该分类的 struct：

```csharp
// 定义分类（在自己的程序集中）
public class GameEventCatalogAttribute : StructSequenceCatalogAttribute
{
    public override string NameSpace => "Game.Events";
    public override string GeneratePath => "Assets/Game/Generated/StructSequence";
}

// 标记 struct（unmanaged — 所有字段均为值类型）
[GameEventCatalog]
public struct MoveEvent
{
    public int entityId;
    public float x, y, z;
}

// 标记 struct（non-unmanaged — 含引用字段）
[GameEventCatalog]
public struct DamageEvent
{
    public int targetId;
    public double damage;
    public string skillName;
}
```

### 2. 生成注册代码

在 Unity 编辑器菜单执行 `Tools/StructSequence/Generate All`。

工具会扫描所有程序集，按分类分组，在指定 `GeneratePath` 下生成三个文件（`partial class`）：

| 文件 | 内容 |
|------|------|
| `{Name}StructSequenceRegister.cs` | `Init()` 注册方法，含 `[RuntimeInitializeOnLoadMethod]` |
| `{Name}StructSequenceRegister_Write.cs` | non-unmanaged struct 的 Write 方法 |
| `{Name}StructSequenceRegister_Read.cs` | non-unmanaged struct 的 Read 方法 |

生成规则：
- **unmanaged struct**（所有字段递归均为值类型）：直接委托给 `UnmanagedStructAccessor<T>`，无需生成读写方法
- **non-unmanaged struct**（含引用字段）：生成逐字段读写代码，payload 采用紧凑顺序布局（值类型字段 `sizeof(T)` 字节，引用字段 4 字节存 ref index）

生成后 Unity 会自动编译，运行时通过 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 在最早阶段完成注册，无需手动调用。

---

### 手动注册（不使用代码生成）

如果不想用代码生成，也可以在启动时手动调用 `UnsafeStructAccessor<T>.Init`。

对于 unmanaged struct：

```csharp
UnsafeStructAccessor<MoveEvent>.Init(
    UnmanagedStructAccessor<MoveEvent>.Size,
    UnmanagedStructAccessor<MoveEvent>.Write,
    UnmanagedStructAccessor<MoveEvent>.Read);
```

对于含引用字段的 struct：

```csharp
unsafe
{
    UnsafeStructAccessor<DamageEvent>.Init(
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
