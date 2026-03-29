# DataVisit 操作手册

## 目录

1. [概述](#概述)
2. [模块结构](#模块结构)
3. [核心概念](#核心概念)
4. [快速开始](#快速开始)
5. [Attribute 参考](#attribute-参考)
6. [代码生成](#代码生成)
7. [序列化协议](#序列化协议)
8. [运行时使用](#运行时使用)
9. [版本兼容性说明](#版本兼容性说明)
10. [API 参考](#api-参考)
11. [注意事项与约束](#注意事项与约束)

---

## 概述

DataVisit 是 ActionFramework 中的**类型访问者（Type Visitor）序列化框架**，基于 Attribute 标记和代码生成，提供无反射的高性能二进制序列化 / 反序列化能力。

**主要特性：**

- 基于 Attribute 标记，零手写序列化代码
- 编辑器端代码生成，运行时无反射开销
- 两套二进制协议：SevenBit（紧凑可变长）和 RawBit（原始定长）
- 支持多态（动态类型）序列化与反序列化
- 支持字段缺省跳过（向前/向后兼容）
- 内置 Reset 访问器用于对象数据清零

**适用场景：**

| 协议 | 适用场景 |
|------|---------|
| SevenBit | 网络传输、持久化存储（体积敏感、需版本兼容） |
| RawBit | 本地缓存、编辑器内部存储（速度优先、schema 固定） |
| Reset | 对象池回收前的字段重置 |

---

## 模块结构

```
Common/
├── Editor/
│   └── DataVisit/
│       ├── DataVisitCodeGenerator.cs   # 代码生成入口（菜单触发）
│       ├── DataVisitTypeCollector.cs    # 类型收集与 Catalog 构建
│       └── VisitCodeGenData.cs          # 代码生成数据结构
│
└── Runtime/
    └── Serialization/
        └── DataVisit/
            ├── IVisitier.cs             # 访问者接口
            ├── TypeVisitT.cs            # 泛型类型注册中心
            ├── InnerTypeVisit.cs        # 内置基础类型注册
            ├── Attribute/
            │   ├── VisitCatalogAttribute.cs     # Catalog 标识
            │   ├── VisitFieldAttribute.cs       # 字段标记
            │   └── VisitTypeTagAttribute.cs     # TypeID 映射标记
            ├── SevenBit/
            │   ├── SevenBitDataType.cs          # 协议类型枚举
            │   ├── SevenBitPackVisitier.cs      # 序列化（Pack）
            │   └── SevenBitUnPackVisitier.cs    # 反序列化（UnPack）
            ├── RawBit/
            │   ├── RawBitDataType.cs            # 协议类型枚举
            │   ├── RawBitPackVisitier.cs        # 序列化（Pack）
            │   └── RawBitUnPackVisitier.cs      # 反序列化（UnPack）
            └── Reset/
                └── ResetVisitier.cs             # 数据重置访问器
```

---

## 核心概念

### Catalog（目录）

Catalog 是类型分组的顶层单元。每个业务模块定义一个 Catalog（继承 `VisitCatalogAttribute`），将该模块所有需要序列化的类型归为一组，统一生成代码。

### FieldIndex（字段索引）

每个可序列化字段通过 `[VisitField(fieldIndex)]` 指定唯一索引，用于二进制协议中字段的定位。**同一类型内 FieldIndex 不允许重复。**

### TypeID（类型标识）

用于多态类型调度。当字段声明为基类但实际存储派生类时，序列化时写入 TypeID，反序列化时根据 TypeID 创建正确的类型实例。

**TypeID 计算公式：**

```
TypeID = (内部TypeID << 8) | TypeIDFieldIndex
```

`TypeIDFieldIndex` 由 Catalog 定义，在低 8 位区分不同 Catalog，高位区分 Catalog 内不同类型。

### Dynamic vs Static

| 模式 | 含义 | 使用场景 |
|------|------|---------|
| Static | 编译期已知具体类型 | 字段类型即实际类型 |
| Dynamic | 运行时多态 | 字段声明为基类，实际可能是任意派生类 |

### Required 标记

`VisitFlag.Required`（值为 1）控制字段是否在默认值时跳过写入：

- **Required（flag 包含 1）**：即使值为默认值（0 / null / empty）也写入
- **非 Required（默认）**：默认值不写入，节省空间

---

## 快速开始

### 第一步：定义 Catalog

为你的模块创建一个 Catalog Attribute，继承 `VisitCatalogAttribute`：

```csharp
using DataVisit;

// TypeIDFieldIndex：当前 Catalog 的 ID 标识位（1~255），不同 Catalog 间不能重复
public class MyModuleCatalogAttribute : VisitCatalogAttribute
{
    public override byte TypeIDFieldIndex => 1;
    public override string NameSpace => "MyModule";
    public override string GeneratePath => "Assets/MyModule/Generated";
}
```

### 第二步：标记数据类型

给需要序列化的 class / struct 打上 Catalog Attribute，给字段打上 `[VisitField]`：

```csharp
[MyModuleCatalog]
public class PlayerData
{
    [VisitField(1)]
    public int id;

    [VisitField(2)]
    public string name;

    [VisitField(3)]
    public float hp;

    [VisitField(4)]
    public List<ItemData> items;
}

[MyModuleCatalog]
public struct ItemData
{
    [VisitField(1)]
    public int itemId;

    [VisitField(2)]
    public int count;
}
```

### 第三步：标记多态字段（可选）

如果字段运行时可能是不同派生类，使用 `[VisitDynamicField]`：

```csharp
[MyModuleCatalog]
public class SkillEffect
{
    [VisitField(1)]
    public int effectId;
}

[MyModuleCatalog]
public class DamageEffect : SkillEffect
{
    [VisitField(2)]
    public float damage;
}

[MyModuleCatalog]
public class HealEffect : SkillEffect
{
    [VisitField(2)]
    public float healAmount;
}

[MyModuleCatalog]
public class SkillData
{
    [VisitField(1)]
    public int skillId;

    // 基类引用，运行时可能是 DamageEffect 或 HealEffect
    [VisitDynamicField(2)]
    public SkillEffect effect;

    // 动态类型列表
    [VisitDynamicField(3)]
    public List<SkillEffect> effects;
}
```

### 第四步：生成代码

在 Unity 编辑器菜单中执行：

```
Tools → DataVisit → Generate All
```

生成结果为两个文件（位于 Catalog 指定的 `GeneratePath`）：

| 文件 | 内容 |
|------|------|
| `MyModuleVisit_Func.cs` | 每个类型的 Visit 方法（逐字段序列化逻辑） |
| `MyModuleVisit.cs` | TypeID 枚举 + Init() 注册方法 |

### 第五步：初始化注册

在程序启动时调用生成的 `Init()` 方法（确保在**任何序列化操作之前**）：

```csharp
// 注册内置类型
InnerTypeVisit.Register();

// 注册你的模块
MyModuleVisit.Init();
```

### 第六步：序列化 / 反序列化

```csharp
// ========== SevenBit 序列化 ==========
var player = new PlayerData { id = 1, name = "Alice", hp = 100f };

// Pack（序列化）
var memory = new MemoryStream();
var packer = new SevenBitPackVisitier(memory);
TypeVisitT<PlayerData>.Visit(packer, 0, "", 0, ref player);
byte[] bytes = memory.ToArray();

// UnPack（反序列化）
var unpacker = new SevenBitUnPackVisitier(bytes);
var loaded = new PlayerData();
TypeVisitT<PlayerData>.Visit(unpacker, 0, "", 0, ref loaded);

// ========== RawBit 序列化 ==========
var rawMemory = new MemoryStream();
var rawPacker = new RawBitPackVisitier(rawMemory);
TypeVisitT<PlayerData>.Visit(rawPacker, 0, "", 0, ref player);
byte[] rawBytes = rawMemory.ToArray();

var rawUnpacker = new RawBitUnPackVisitier(rawBytes);
var rawLoaded = new PlayerData();
TypeVisitT<PlayerData>.Visit(rawUnpacker, 0, "", 0, ref rawLoaded);

// ========== Reset（数据清零）==========
TypeVisitT<PlayerData>.Visit(ResetVisitier.Default, 0, "", 0, ref player);
// player 的所有字段已被重置为默认值
```

---

## Attribute 参考

### VisitCatalogAttribute

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public abstract class VisitCatalogAttribute : Attribute
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `TypeIDFieldIndex` | `byte` | Catalog 唯一标识（1~255），嵌入 TypeID 低 8 位。不同 Catalog 间不可重复 |
| `NameSpace` | `string` | 生成代码的命名空间 |
| `GeneratePath` | `string` | 生成文件的输出目录（相对于项目根目录） |

**使用方式：** 继承此类，为每个业务模块创建一个具体 Attribute，然后标注在数据类/结构体上。

---

### VisitFieldAttribute

```csharp
[AttributeUsage(AttributeTargets.Field)]
public class VisitFieldAttribute : Attribute
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `fieldIndex` | `int` | 字段在类型内的唯一索引（SevenBit 中用作 tag） |
| `tag` | `uint` | 可选标记位（默认 0），传递给 IVisitier 的 flag 参数 |

**约束：** 仅作用于 `public` 实例字段。同一类型内 `fieldIndex` 不可重复。

---

### VisitDynamicFieldAttribute

```csharp
public class VisitDynamicFieldAttribute : VisitFieldAttribute
```

继承自 `VisitFieldAttribute`，用于标记多态字段。生成代码时会调用 `VisitDynamicClass` / `VisitDynamicList` / `VisitDynamicArray` 等动态版本方法。

**适用场景：** 字段声明类型是基类或接口，实际运行时可能存储任意派生类型。

---

### VisitTypeTagAttribute / VisitTypeIDCatalogAttribute

这两个 Attribute 由代码生成器自动生成在 TypeID 枚举上，**用户不需手动使用**。它们的作用是在重新生成代码时保持已有的 TypeID 映射不变，避免破坏序列化兼容性。

---

## 代码生成

### 触发方式

```
Unity 菜单 → Tools → DataVisit → Generate All
```

### 生成流程

```
1. 扫描所有程序集，收集标记了 VisitCatalogAttribute 的类型
2. 查找已有的 TypeID 枚举（[VisitTypeIDCatalog] 标记），恢复旧的 TypeID 映射
3. 按 Catalog 分组，处理基类关系和字段信息
4. 为新发现的 class 类型分配 TypeID
5. 检查 FieldIndex / TypeIDFieldIndex 冲突
6. 生成 Visit 方法文件（_Func.cs）
7. 生成 TypeID 枚举 + Init 注册文件（.cs）
```

### 生成代码示例

对于上面的 `PlayerData`，生成的 Visit 方法大致如下：

```csharp
// MyModuleVisit_Func.cs（自动生成，勿手动修改）
using DataVisit;

namespace MyModule
{
    public partial class MyModuleVisit
    {
        private static void VisitPlayerData(IVisitier visitier, uint tag, string name, uint flag, ref PlayerData value)
        {
            visitier.Visit(1, nameof(value.id), 0, ref value.id);
            visitier.Visit(2, nameof(value.name), 0, ref value.name);
            visitier.Visit(3, nameof(value.hp), 0, ref value.hp);
            visitier.VisitList(4, nameof(value.items), 0, ref value.items);
        }

        private static void VisitItemData(IVisitier visitier, uint tag, string name, uint flag, ref ItemData value)
        {
            visitier.Visit(1, nameof(value.itemId), 0, ref value.itemId);
            visitier.Visit(2, nameof(value.count), 0, ref value.count);
        }
    }
}
```

```csharp
// MyModuleVisit.cs（自动生成，勿手动修改）
using DataVisit;

namespace MyModule
{
    public partial class MyModuleVisit
    {
        [VisitTypeIDCatalog(typeof(MyModuleCatalogAttribute))]
        public enum TypeID
        {
            [VisitTypeTag(typeof(PlayerData))]
            PlayerData = 0x101,
            [VisitTypeTag(typeof(SkillEffect))]
            SkillEffect = 0x201,
            // ...
        }

        private static bool isInit = false;
        public static void Init()
        {
            if (isInit) return;
            isInit = true;
            TypeVisitT<ItemData>.RegisterStruct(VisitItemData);
            TypeVisitClassT<PlayerData>.Register((int)TypeID.PlayerData, VisitPlayerData);
            // ...
        }
    }
}
```

### 重新生成的兼容性

重新执行 Generate All 时，代码生成器会读取旧的 TypeID 枚举上的 `[VisitTypeTag]` 标记，**保持已有类型的 TypeID 不变**，仅为新增类型分配新 ID。这确保了旧数据仍可正确反序列化。

---

## 序列化协议

### SevenBit 协议

紧凑的可变长编码，支持字段缺省跳过，具有前后向兼容能力。

#### 数据类型

| 枚举值 | 名称 | 说明 |
|--------|------|------|
| 0 | Positive | 正整数（7-bit 可变长编码） |
| 1 | Negative | 负整数（绝对值用 7-bit 编码） |
| 2 | Float | float（bit 转 uint 后 7-bit 编码） |
| 3 | Double | double（bit 转 ulong 后 7-bit 编码） |
| 4 | String | 字符串 / 字节数组（长度 + 原始字节） |
| 5 | Vector | 容器（长度 + 逐元素编码） |
| 6 | StructBegin | 结构体/类开始标记 |
| 7 | DynamicBegin | 动态类型开始标记（含 TypeID） |
| 8 | StructEnd | 结构体/类结束标记 |

#### Header 格式

```
┌───────────┬────────────┐
│  type(4b) │  tag(4b)   │  1 字节
└───────────┴────────────┘
```

- 高 4 位：`SevenBitDataType` 枚举
- 低 4 位：tag 值（0~14 直接内嵌，15 表示后续用变长编码）

#### 缺省跳过规则

当字段无 `Required` 标记且值为默认值时（数值为 0、字符串为 null/empty、对象为 null），**不写入任何字节**。反序列化时通过 `SkipToTag` 按 tag 顺序查找，找不到则保留默认值。

> **注意：** 同一 `StructBegin`/`StructEnd` 块内的字段 tag 必须升序排列。

#### 动态类型编码结构

```
DynamicBegin(tag)
  ├── Positive(0) → TypeID（必写）
  ├── StructBegin(1)
  │     └── [字段数据...]
  │     └── StructEnd
  └── StructEnd
```

---

### RawBit 协议

原始定长二进制编码，不支持字段缺省跳过，速度最快。

#### 特点

- 所有字段按声明顺序紧密排列，无 header / tag 开销
- 数值类型直接 `memcpy`（小端序）
- 字符串和数组以 `int32 长度 + 原始字节` 编码（长度 = -1 表示 null）
- 多态类型通过 1 字节 marker 区分：

| Marker | 含义 |
|--------|------|
| `0 (Null)` | null 引用 |
| `1 (Static)` | 静态类型实例 |
| `2 (Dynamic)` | 动态类型实例（后跟 int32 TypeID） |

#### 兼容性

RawBit **不支持字段增删后的向前/向后兼容**。schema 变更后必须重新生成所有数据。

---

## 运行时使用

### 类型注册

#### 注册顺序

```csharp
// 1. 先注册内置基础类型
InnerTypeVisit.Register();

// 2. 再注册各模块（顺序无要求）
MyModuleVisit.Init();
AnotherModuleVisit.Init();
```

`Init()` 内部有 `isInit` 守卫，重复调用安全。

#### 注册原理

| 类型 | 注册方式 | 说明 |
|------|---------|------|
| struct | `TypeVisitT<T>.RegisterStruct(visit)` | 设置 VisitFunc + IsCustomStruct 标记 |
| class | `TypeVisitClassT<T>.Register(id, visit)` | 设置 VisitFunc + 注册到全局 TypeID 字典 |
| 内置类型 | `InnerTypeVisit.Register()` | bool/byte/int/float/string 等直接转发 |

---

### 序列化 API

#### SevenBit 序列化

```csharp
// 序列化
var stream = new MemoryStream();
var packer = new SevenBitPackVisitier(stream);
TypeVisitT<MyType>.Visit(packer, 0, "", 0, ref data);
byte[] result = stream.ToArray();

// 反序列化
var unpacker = new SevenBitUnPackVisitier(result);
var output = new MyType();
TypeVisitT<MyType>.Visit(unpacker, 0, "", 0, ref output);
```

#### RawBit 序列化

```csharp
// 序列化
var stream = new MemoryStream();
var packer = new RawBitPackVisitier(stream);
TypeVisitT<MyType>.Visit(packer, 0, "", 0, ref data);
byte[] result = stream.ToArray();

// 反序列化
var unpacker = new RawBitUnPackVisitier(result);
var output = new MyType();
TypeVisitT<MyType>.Visit(unpacker, 0, "", 0, ref output);
```

#### ArraySegment 支持

两种 UnPack 访问器均支持 `ArraySegment<byte>` 构造，可避免子数组拷贝：

```csharp
var segment = new ArraySegment<byte>(buffer, offset, length);
var unpacker = new SevenBitUnPackVisitier(segment);
```

#### 数据重置

```csharp
TypeVisitT<MyType>.Visit(ResetVisitier.Default, 0, "", 0, ref data);
```

将对象所有字段设为默认值（数值 → 0，字符串 → `""`，数组 → 空数组，容器 → Clear）。

---

## 版本兼容性说明

### SevenBit 兼容性矩阵

| 场景 | 是否兼容 | 说明 |
|------|---------|------|
| 新增字段（新写旧读） | ✅ | 旧版反序列化时跳过未知 tag |
| 删除字段（旧写新读） | ✅ | 新版找不到 tag，保持默认值 |
| 修改字段类型 | ❌ | tag 不变但类型不匹配，抛异常 |
| 修改 FieldIndex | ❌ | 等同于删旧增新，旧数据对应字段丢失 |
| 修改类名（保持 TypeID） | ✅ | 代码生成器通过 VisitTypeTag 保持 ID 映射 |
| 删除多态子类 | ⚠️ | 反序列化时跳过未注册的 TypeID |

### RawBit 兼容性

RawBit 无 tag / 类型信息，**任何 schema 变更都不兼容。**

### 保持兼容的要点

1. **不要修改已有字段的 FieldIndex**
2. **不要修改 Catalog 的 TypeIDFieldIndex**
3. 新增字段使用新的 FieldIndex（建议递增）
4. 废弃字段：保留 FieldIndex 占位，不要复用
5. 重新生成代码前确认旧 TypeID 枚举存在（避免 ID 重分配）

---

## API 参考

### IVisitier 接口

所有序列化/反序列化/重置操作的统一接口。每个方法签名：

```csharp
void Method(uint tag, string name, uint flag, ref T value);
```

| 参数 | 说明 |
|------|------|
| `tag` | 字段索引（对应 FieldIndex），用于 SevenBit 协议的字段定位 |
| `name` | 字段名（调试用，不影响序列化） |
| `flag` | 标记位，`VisitFlag.Required = 1` 表示必写 |
| `value` | 字段引用，序列化时读取、反序列化时写入 |

#### 支持的方法

| 方法 | 支持类型 |
|------|---------|
| `Visit` | `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `string`, `bool[]`, `byte[]`, `sbyte[]` |
| `VisitEnum<T>` | 所有 `enum` 类型 |
| `VisitStruct<T>` | 自定义 `struct` |
| `VisitClass<T>` | 自定义 `class`（静态类型） |
| `VisitDynamicClass<T>` | 自定义 `class`（多态） |
| `VisitArray<T>` / `VisitDynamicArray<T>` | 数组 |
| `VisitList<T>` / `VisitDynamicList<T>` | `List<T>` |
| `VisitDictionary<K,V>` / `VisitDynamicDictionary<K,V>` | `Dictionary<K,V>` |
| `VisitHashSet<T>` | `HashSet<T>` |

---

### TypeVisitT\<T\> 静态类

```csharp
// 执行访问（序列化/反序列化/重置 取决于 visitier 类型）
static void Visit(IVisitier visitier, uint tag, string name, uint flag, ref T value);

// 注册 struct 类型的访问函数
static void RegisterStruct(VisitDelegate visit);

// 自定义创建函数（默认 default(T)）
static CreatorDelegate New;

// 是否为自定义结构体
static bool IsCustomStruct;
```

### TypeVisitClassT\<T\> 静态类

```csharp
// 注册 class 类型：TypeID + 访问函数
static void Register(int id, VisitDelegate visit);
```

### TypeVisit 静态类

```csharp
// 获取对象的 TypeID（用于动态类型调度）
static int GetTypeId(object v);

// 根据 TypeID 获取访问信息
static TypeVisitInfo GetVisit(int typeId);
```

---

## 注意事项与约束

### 字段约束

- **仅支持 `public` 实例字段**，不支持 property、private 字段、static 字段
- 未标记 `[VisitField]` 的字段不会被序列化
- `[VisitField]` 的 `fieldIndex` 在 SevenBit 中要求**同一类型内升序排列**

### 继承约束

- 生成的 Visit 方法使用 `DeclaredOnly`，仅处理当前类声明的字段
- 基类字段通过对基类的递归 `VisitClass` 调用处理
- 基类也需要标记 `[VisitCatalog]` 并包含 `[VisitField]`

### 容器约束

- `Dictionary` 的 Key 类型必须是基础类型或已注册的自定义类型
- Dynamic 版本容器（`VisitDynamicList` 等）的元素类型约束为 `class, new()`
- `HashSet<T>` 不支持 Dynamic 元素

### 初始化约束

- `InnerTypeVisit.Register()` 必须在所有模块 `Init()` 之前调用
- 序列化/反序列化操作前必须确保相关类型已经注册
- 同一类型不可重复注册（会抛出异常）

### 代码生成约束

- 生成的文件不可手动修改（下次生成会覆盖）
- 生成路径（`GeneratePath`）需在对应模块的 Assembly Definition 范围内
- 重新生成前确保 Unity 已编译通过（依赖反射扫描当前程序集）
