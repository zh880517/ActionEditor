# EasyConfig Excel 关联配置拆分设计

## 背景和目标

当前 `EasyConfig` 的 Excel 配置以配置类型为最小导出和加载单位：

- `ListConfig<T>` 通过 `ConfigListCollector<T>` 持有列表数据。
- `DictionaryConfig<TKey, T>` 通过 `ConfigDictionaryCollector<TKey, T>` 持有字典数据。
- `ExcelBinaryTypeCollector` 根据 `ConfigGroupAttribute` 收集配置类型。
- `ExcelBinaryCodeGenerator` 为每个分组生成 `LoadAll` 和 `Clear`。
- `ExcelBinaryExportUtil` 按配置类型导出独立二进制文件。

新需求是在不改变 Excel 表格维护方式的前提下，让同一个 Excel 页签、同一行数据可以投影成多份独立配置数据：

- 主配置保存一组核心字段，用于独立导出和校验。
- 关联配置保存另一组字段，并在加载后引用对应的主配置对象。
- 主配置和关联配置可以来自同一个 Excel 页签、同一行数据。
- 二进制导出时必须按配置类型导出为多份文件，避免某一组字段变化影响另一组字段的二进制校验。

本文档只描述框架工具层能力：Excel 行数据拆分、类型分类、二进制导出和加载后关联。`Logic`、`View`、表现层、逻辑层等属于应用层命名，不进入 EasyConfig 的框架 API 和生成器概念。

## 当前架构理解

Excel 配置读取和二进制导出已经具备可复用基础：

- 字段读取由 `ColumnReaderUtil`、`StructColumnReader` 和字段 Attribute 完成。
- 页签绑定由 `ExcelSheetAttribute` 完成。
- Dictionary Key 由 `KeyColumnAttribute` 完成。
- 二进制分组由 `ConfigGroupAttribute` 完成。
- 生成类已经是应用层加载配置的入口。

因此本设计不引入新的全局加载调度器，也不改变 Excel 列读取规则，而是扩展现有 Excel Binary 类型识别和代码生成能力。

## 术语和边界

| 术语 | 含义 |
| --- | --- |
| 主配置 | 独立配置类型，仍然继承 `ListConfig<T>` 或 `DictionaryConfig<TKey, T>` |
| 关联配置 | 继承新增关联模板类的配置类型，加载后引用对应主配置 |
| 主配置类型 | 关联配置声明依赖的目标配置类型 |
| Key 关联 | Dictionary 关联配置按相同 `KeyColumnAttribute` 对应的 Key 查找主配置 |
| Index 关联 | List 关联配置按相同行索引查找主配置 |
| 生成分组 | 由 `ConfigGroupAttribute` 声明并由 `ExcelBinaryCodeGenerator` 输出的生成类 |

边界：

- 不改变 Excel 表格格式，允许同一个页签由多个配置类型读取。
- 不要求主配置和关联配置位于同一个 `ConfigGroupAttribute` 分组。
- 不新增手动调用的公开 `LinkAll` 接口。
- 不引入全局依赖图加载器。
- 不新增 `.asmdef` 文件。
- 第一版每个关联配置只引用一个主配置类型。
- 第一版不支持宽松关联模式。
- 关联配置二进制不包含主配置对象数据。

## 方案概览

新增两个 Runtime 模板类：

```csharp
public class LinkedDictionaryConfig<TKey, TLinked, TPrimary> : DictionaryConfig<TKey, TLinked>
    where TKey : struct
    where TLinked : LinkedDictionaryConfig<TKey, TLinked, TPrimary>
    where TPrimary : IDictionaryConfig
{
    public TPrimary Primary { get; internal set; }
}

public class LinkedListConfig<TLinked, TPrimary> : ListConfig<TLinked>
    where TLinked : LinkedListConfig<TLinked, TPrimary>
    where TPrimary : IListConfig
{
    public TPrimary Primary { get; internal set; }
}
```

使用示例：

```csharp
[ExcelSheet("Skill")]
[KeyColumn("Id")]
[GameConfigGroup]
public class SkillCoreConfig : DictionaryConfig<int, SkillCoreConfig>
{
    public int Id;
    public int Damage;
    public float Cooldown;
}

[ExcelSheet("Skill")]
[KeyColumn("Id")]
[GameExtraConfigGroup]
public class SkillDisplayConfig : LinkedDictionaryConfig<int, SkillDisplayConfig, SkillCoreConfig>
{
    public int Id;
    public string Icon;
    public string DisplayName;
}
```

`SkillCoreConfig` 和 `SkillDisplayConfig` 可以读取同一个 `Skill` 页签，但各自只读取自己声明的字段。导出二进制时生成两份文件，加载 `SkillDisplayConfig` 所在分组时自动恢复 `SkillDisplayConfig.Primary` 引用。

这里的 `Core`、`Display` 只是应用层示例命名。框架只知道一个配置类型关联到另一个配置类型，不判断这些字段属于哪一层业务。

## 类型识别

`ExcelBinaryTypeCollector` 需要从当前的二分类型扩展为明确的配置类型分类：

```text
ConfigKind.List
ConfigKind.Dictionary
ConfigKind.LinkedList
ConfigKind.LinkedDictionary
```

`ConfigTypeData` 建议增加字段：

```text
Type Type
ConfigKind Kind
Type KeyType
Type PrimaryType
```

识别规则：

- 继承 `ListConfig<T>` 且泛型参数为自身：`List`。
- 继承 `DictionaryConfig<TKey, T>` 且第二个泛型参数为自身：`Dictionary`。
- 继承 `LinkedListConfig<TLinked, TPrimary>` 且 `TLinked` 为自身：`LinkedList`。
- 继承 `LinkedDictionaryConfig<TKey, TLinked, TPrimary>` 且 `TLinked` 为自身：`LinkedDictionary`。

校验规则：

- `LinkedDictionary` 的 `TPrimary` 必须继承 `DictionaryConfig<TKey, TPrimary>`，并使用相同 `TKey`。
- `LinkedList` 的 `TPrimary` 必须继承 `ListConfig<TPrimary>`。
- 关联配置和主配置可以读取同一个页签，也可以读取不同页签；框架只根据 Key 或 Index 建立对象引用。
- 校验失败时输出 `Debug.LogError`，并跳过该关联配置类型。

## 二进制导出

`ExcelBinaryExportUtil` 继续按类型导出文件：

- `List` 和 `LinkedList` 序列化 `ConfigListCollector<T>.Configs`。
- `Dictionary` 和 `LinkedDictionary` 序列化 `ConfigDictionaryCollector<TKey, T>.Configs`。

关联模板类中的 `Primary` 不添加项目二进制序列化所需的字段 Attribute，因此不会进入关联配置二进制。业务侧也不应在 `Primary` 上添加序列化标记。

## 生成代码

`ExcelBinaryCodeGenerator` 继续为每个 `ConfigGroupAttribute` 分组生成注册类。生成类保留现有公开接口：

```csharp
public static void LoadAll(IConfigBytesProvider provider, IConfigSerializer serializer)
public static void Clear()
```

当当前分组包含 `LinkedList` 或 `LinkedDictionary` 时，额外生成私有关联方法，并在 `LoadAll` 末尾自动调用：

```csharp
public static void LoadAll(IConfigBytesProvider provider, IConfigSerializer serializer)
{
    // 反序列化并填充 Collector
    LinkAll();
}

private static void LinkAll()
{
    // 仅生成当前分组内关联配置类型的对象引用恢复代码
}
```

当当前分组不包含关联配置类型时，不生成 `LinkAll`，`LoadAll` 也不调用关联逻辑。

### LinkedDictionary 关联

关联逻辑按相同 Key 查找主配置：

```text
foreach linked in ConfigDictionaryCollector<TKey, TLinked>.Configs
    if ConfigDictionaryCollector<TKey, TPrimary>.Configs.TryGetValue(linked.Key, out primary)
        linked.Value.Primary = primary
    else
        Debug.LogError(...)
```

### LinkedList 关联

关联逻辑按相同 Index 查找主配置：

```text
for i in ConfigListCollector<TLinked>.Configs
    if i < ConfigListCollector<TPrimary>.Configs.Count
        ConfigListCollector<TLinked>.Configs[i].Primary = ConfigListCollector<TPrimary>.Configs[i]
    else
        Debug.LogError(...)
```

关联失败采用严格模式：只输出错误，缺失项的 `Primary` 保持默认值，不提供宽松模式开关。

## 加载顺序

主配置和关联配置可以位于不同 `ConfigGroupAttribute` 分组。框架不负责跨组依赖排序，应用层必须保证调用顺序：

```text
主配置分组 LoadAll
关联配置分组 LoadAll
关联配置分组 LoadAll 内部自动 LinkAll
```

如果关联配置分组加载时主配置分组尚未加载，关联阶段会按缺失 Key 或缺失 Index 输出错误。

## 与热刷新设计的关系

本设计的核心能力位于 Runtime 模板类和 Excel Binary 生成链路。Editor Excel 热刷新如果重新读取关联配置 Collector，也需要重新执行同样的关联规则。

后续实现热刷新时应复用生成器产生的关联逻辑，或在 Editor 热刷新派发流程中调用同等关联入口，避免 Editor 侧复制一份独立关联实现。

## 命名迁移

如果早期草案或业务代码已经使用 `Logic` / `View` 命名，迁移时只保留应用层类名的自由度，不把这些词固化到框架 API：

- `ListViewConfig<TView, TLogic>` 改为 `LinkedListConfig<TLinked, TPrimary>`。
- `ViewConfig<TKey, TView, TLogic>` 改为 `LinkedDictionaryConfig<TKey, TLinked, TPrimary>`。
- `ConfigKind.ListView` 改为 `ConfigKind.LinkedList`。
- `ConfigKind.DictionaryView` 改为 `ConfigKind.LinkedDictionary`。
- 生成方法中的 `BindAll` 改为 `LinkAll`。
- 关联属性从 `Logic` 改为 `Primary`。

应用层仍可把自己的类型命名为 `SkillLogicConfig`、`SkillViewConfig` 等，但 EasyConfig 工具层只按“关联配置引用主配置”处理。

## 编译检查

本阶段只要求保证代码编译成功：

- Runtime 新增模板类不引用 `UnityEditor`。
- Editor 生成代码可以引用 `UnityEngine.Debug` 输出关联错误。
- 生成代码在没有关联配置类型的分组中不生成无用关联方法。
- 生成代码在存在跨组主配置引用时仍能编译，因为 Collector 是全局静态容器。

## 开发任务

### Runtime 模板类

- [ ] 新增 `LinkedDictionaryConfig<TKey, TLinked, TPrimary>`。
- [ ] 新增 `LinkedListConfig<TLinked, TPrimary>`。
- [ ] `Primary` 属性使用 `internal set`，避免业务侧在外部随意改写关联。
- [ ] 确认 Runtime 模板类不引用 `UnityEditor`。

### 类型识别

- [ ] 新增或扩展 `ConfigKind`：`List`、`Dictionary`、`LinkedList`、`LinkedDictionary`。
- [ ] 扩展配置类型元数据，包含 `Type`、`Kind`、`KeyType`、`PrimaryType`、`SheetName`。
- [ ] 识别 `LinkedListConfig<TLinked, TPrimary>`，并校验 `TLinked` 为自身。
- [ ] 识别 `LinkedDictionaryConfig<TKey, TLinked, TPrimary>`，并校验 `TLinked` 为自身。
- [ ] 校验 `LinkedList` 的 `PrimaryType` 继承 `ListConfig<TPrimary>`。
- [ ] 校验 `LinkedDictionary` 的 `PrimaryType` 继承相同 `TKey` 的 `DictionaryConfig<TKey, TPrimary>`。
- [ ] 类型校验失败时输出 `Debug.LogError`，并跳过该类型。

### 二进制导出

- [ ] `LinkedList` 继续按 `ConfigListCollector<TLinked>.Configs` 导出。
- [ ] `LinkedDictionary` 继续按 `ConfigDictionaryCollector<TKey, TLinked>.Configs` 导出。
- [ ] 确认 `Primary` 不进入关联配置二进制数据。
- [ ] 确认同一 Excel 页签可以被主配置和关联配置分别读取。

### 生成代码和关联恢复

- [ ] 扩展 `ExcelBinaryCodeGenerator`，在包含关联配置的分组中生成 `LinkAll`。
- [ ] `LoadAll` 在当前分组反序列化完成后自动调用 `LinkAll`。
- [ ] 没有关联配置的分组不生成 `LinkAll`。
- [ ] `LinkedDictionary` 按相同 Key 从主配置 Collector 查找并写入 `Primary`。
- [ ] `LinkedList` 按相同行索引从主配置 Collector 查找并写入 `Primary`。
- [ ] 关联失败时输出错误，缺失项的 `Primary` 保持默认值。
- [ ] 跨分组主配置尚未加载时，保持严格错误提示，不自动加载主配置分组。

### 验证

- [ ] Unity 编译通过。
- [ ] 普通 `ListConfig<T>` / `DictionaryConfig<TKey, T>` 原有导出和加载不受影响。
- [ ] `LinkedList` / `LinkedDictionary` 可以生成独立二进制文件。
- [ ] 加载关联配置分组后，`Primary` 引用恢复正确。
- [ ] 主配置缺失时有明确错误日志。
