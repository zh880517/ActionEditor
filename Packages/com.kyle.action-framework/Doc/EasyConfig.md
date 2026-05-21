# EasyConfig 操作手册

## 概述

EasyConfig 是一个面向游戏配置的管理框架，支持两套独立的配置模式：

- **Excel 配置模式**：以 Excel 表格为数据源，编辑器自动监听文件变更并导出缓存，运行时通过 `ListConfig<T>` / `DictionaryConfig<TKey,T>` 按行/键访问。
- **Entity 配置模式**：以 ScriptableObject 为数据容器，通过 `EntityConfig` + `ConfigComponent` 组合体描述实体配置，配合自定义 Inspector 可视化编辑。
- **运行时加载器**：两种模式均通过 `ConfigLoaderManager` + `IDataProvider` 解耦数据来源，Editor 和 Runtime 可无缝切换。

---

## 目录结构

```
EasyConfig/
├── Runtime/
│   ├── Excel/
│   │   ├── IConfig.cs                  # IListConfig / IDictionaryConfig 接口
│   │   ├── ListConfig.cs               # List 配置访问基类
│   │   ├── DictionaryConfig.cs         # Dictionary 配置访问基类
│   │   ├── ConfigListCollector.cs      # List 配置全局容器
│   │   ├── ConfigDictionaryCollector.cs# Dictionary 配置全局容器
│   │   └── Attribute/
│   │       ├── ExcelSheetAttribute.cs  # 标记配置所在页签名
│   │       ├── KeyColumnAttribute.cs   # 标记 Dictionary 的 Key 列名
│   │       ├── ColumnNameAttribute.cs  # 覆写字段对应的列名
│   │       ├── FieldSeparatorAttribute.cs  # 列表/结构体字段的分隔符
│   │       ├── DictionarySeparatorAttribute.cs  # Dictionary 字段的分隔符
│   │       ├── DynimaicListAttribute.cs    # 动态列表（多列映射到一个字段）
│   │       ├── NameIndexAttribute.cs   # 列名索引基类
│   │       └── ConfigIndexAttribute.cs # 结构体内联列标记
│   ├── Entity/
│   │   ├── IEntityConfig.cs            # 实体配置运行时接口
│   │   └── IConfigComponent.cs         # 配置组件运行时标记接口
│   └── Loader/
│       ├── IDataProvider.cs            # 数据提供者接口
│       ├── IConfigLoader.cs            # 配置加载器接口
│       ├── ConfigLoaderManager.cs      # 单例调度中心
│       ├── ConfigLoader.cs             # 带缓存的泛型加载器基类
│       └── TConfigLoader.cs            # 单例加载器泛型基类
└── Editor/
    ├── Excel/
    │   ├── ExcelToCache.cs             # Excel → JSON 缓存导出器
    │   ├── ExcelReadCache.cs           # 缓存元数据（LastWriteTime 等）
    │   ├── SheetData.cs / RowData.cs   # 中间 JSON 数据结构
    │   ├── IExcelExportFilter.cs       # 页签/文件名过滤接口
    │   ├── ExportUtil.cs               # 从缓存 JSON 填充 Collector 的工具
    │   ├── Monitor/
    │   │   ├── ExcelDataManager.cs     # 文件监听 & 热更新调度（ScriptableSingleton）
    │   │   ├── ExcelDataCollector.cs   # 数据收集器抽象基类
    │   │   └── DictionaryDataCollectorT.cs # 泛型 Dictionary 收集器（含编辑器搜索支持）
    │   └── ColumnReader/               # 列读取器（支持基础类型、List、Struct）
    │       ├── ColumnReader.cs
    │       ├── ListColumnReader.cs
    │       ├── StructColumnReader.cs
    │       ├── DynamicListColumnReader.cs
    │       ├── ColumnReaderUtil.cs
    │       └── IColumnReader.cs
    ├── DataProvider/
    │   ├── EditorDataProvider.cs       # Editor 模式数据提供者基类
    │   └── TAssetExportDataProvider.cs # 基于 ScriptableObject 导出的数据提供者
    └── Entity/
        ├── EntityConfig.cs             # 实体配置 ScriptableObject 编辑器基类
        ├── ConfigComponent.cs          # 配置组件 ScriptableObject 编辑器基类
        └── EntityConfigEditor.cs       # EntityConfig 自定义 Inspector
```

---

## 一、Excel 配置模式

### 1.1 定义配置数据类型

#### List 配置（按行遍历）

```csharp
[ExcelSheet("Skill")]           // 对应 Excel 中页签名为 "Skill"
public struct SkillConfig : IListConfig
{
    public int Id;               // 列名与字段名相同（区分大小写）
    public string Name;
    public float Cooldown;

    [ColumnName("ATK")]          // 实际列名为 "ATK"，字段名随意
    public int AttackPower;

    [FieldSeparator('|')]        // 该列内容以 '|' 分隔，映射为数组
    public int[] LevelValues;

    [DictionarySeparator(';', ':')]  // 元素间用 ';'，键值间用 ':'
    public Dictionary<int, float> BonusMap;
}
```

#### Dictionary 配置（按 Key 索引）

```csharp
[ExcelSheet("Item")]
[KeyColumn("Id")]               // Id 列作为字典 Key
public struct ItemConfig : IDictionaryConfig
{
    public int Id;
    public string Name;
    public int MaxStack;
}
```

### 1.2 Excel 表格格式约定

| 规则 | 说明 |
|------|------|
| 首行非空行 | 作为标题行，每个格子对应一个字段名 |
| `#` 开头的行 | 被忽略（注释行） |
| 完全空行 | 被忽略 |
| 多列同名 | 自动视为数组/List 的多个索引（`ArrayIndex` 递增） |
| 结构体列 | 列名以 `.` 结尾（如 `Pos.`），子列命名为 `Pos.x`、`Pos.y` |

**示例表格（Skill 页签）：**

| Id | Name | Cooldown | ATK | LevelValues | LevelValues | LevelValues |
|----|------|----------|-----|-------------|-------------|-------------|
| 1  | 火球 | 8.0      | 50  | 100         | 200         | 300         |
| 2  | 冰锥 | 5.0      | 30  | 80          | 160         |             |

> 三列 `LevelValues` 会被自动合并为 `int[]`，空格跳过。

### 1.3 运行时访问

```csharp
// List 配置
int count = SkillConfig.Count;
SkillConfig skill = SkillConfig.Get(0);                         // 按索引
SkillConfig found = SkillConfig.Find(s => s.Id == 101);        // 线性查找
bool exists = SkillConfig.Exists(s => s.Name == "火球");
IReadOnlyList<SkillConfig> all = SkillConfig.Configs;

// Dictionary 配置
ItemConfig item = ItemConfig.Get(1001);                         // 按 Key
bool has = ItemConfig.Contains(1001);
IReadOnlyDictionary<int, ItemConfig> all = ItemConfig.Configs;
```

### 1.4 动态列表（DynimaicListAttribute）

当 List 内每个元素有多个字段、且行数不固定时，使用 `[DynimaicList]`：

```csharp
public struct RewardGroup : IListConfig
{
    // 表中以 "Reward" 为列名的多列，每列可以是 "100|5"（ItemId|Count）
    [DynimaicList("Reward", '|')]
    public RewardItem[] Rewards;
}

public struct RewardItem
{
    public int ItemId;
    public int Count;
}
```

---

## 二、编辑器 Excel 监听与导出

### 2.1 初始化 ExcelDataManager

```csharp
// 在 Editor 启动入口（InitializeOnLoadMethod）指定 Excel 目录
[InitializeOnLoadMethod]
private static void Init()
{
    ExcelDataManager.ExcelPath = "D:/Project/Excels/";
    // 可选：设置自定义过滤器（默认导出全部页签）
    ExcelDataManager.instance.ExportFilter = new MyFilter();
}

// 自定义过滤器示例
public class MyFilter : IExcelExportFilter
{
    public bool CheckExcelName(string name) => !name.StartsWith("_");  // 跳过 _ 开头
    public bool CheckSheetName(string name) => !name.StartsWith("#");  // 跳过 # 开头
}
```

### 2.2 创建 DictionaryDataCollectorT

`DictionaryDataCollectorT` 是持久化到 `ScriptableObject` 的 Dictionary 收集器，编辑器中可直接查看数据，并支持搜索下拉列表。

```csharp
// 定义收集器
[CreateAssetMenu(menuName = "Config/ItemCollector")]
public class ItemDataCollector : DictionaryDataCollectorT<ItemDataCollector, ItemConfig, int>
{
    // 在 Inspector 中显示的可读名称（用于搜索下拉）
    protected override string GetShowName(ItemConfig item) => item.Name;
}
```

创建对应 Asset 后，每当监听到 Excel 变动，收集器会自动重新读取对应 JSON 缓存。

**编辑器访问：**

```csharp
// 按 Key 查找
if (ItemDataCollector.TryFind(1001, out ItemConfig item))
    Debug.Log(item.Name);

// 获取搜索列表（用于下拉 UI）
IReadOnlyList<KeyValuePair<int, string>> list = ItemDataCollector.SearchList;
```

### 2.3 ExportUtil — 手动填充 Collector

若不想使用 `ScriptableObject` 收集器，可通过 `ExportUtil` 直接从 JSON 缓存填充全局 `ConfigListCollector` / `ConfigDictionaryCollector`：

```csharp
// cachePath 为 ExcelToCache 的输出目录（默认 Library/ExcelCache/）
ExportUtil.Read<SkillConfig>(cachePath);
ExportUtil.Read<int, ItemConfig>(cachePath);
```

---

## 三、运行时加载器（ConfigLoader）

`ConfigLoader` 系统将配置的**反序列化逻辑**与**数据来源**分离，适用于需要从二进制流加载配置的场景。

### 3.1 定义 ConfigLoader

```csharp
public class SkillLoader : TConfigLoader<SkillLoader, SkillData>
{
    public override string TypeName => "Skill";  // 用于 ConfigLoaderManager 路由

    protected override SkillData ToData(byte[] data)
    {
        // 自定义反序列化逻辑（例如用框架的 Binary Packing）
        var reader = new BitReader(data);
        return new SkillData { Id = reader.ReadInt(), Name = reader.ReadString() };
    }
}
```

### 3.2 访问数据

```csharp
// 初始化时确保 DataProvider 已注册（见第四节）
SkillData skill = SkillLoader.Get("skill_101");
```

`Get` 内部按名称懒加载：首次调用通过 `ConfigLoaderManager` 请求 `IDataProvider`，成功后缓存；后续调用直接返回缓存。

### 3.3 热更新

```csharp
// 某条配置数据发生变化时，通知对应 Loader 更新缓存
ConfigLoaderManager.OnDataModify("Skill", "skill_101", newBytes);
```

### 3.4 Destroy

```csharp
// 清理所有 Loader 缓存（场景切换 / 游戏退出时调用）
ConfigLoaderManager.Destroy();
// 也可清理单个 Loader：
SkillLoader.Destroy();
```

---

## 四、数据提供者（IDataProvider）

`IDataProvider` 是 `ConfigLoaderManager` 与实际文件系统/Asset 之间的桥接接口，需在游戏启动时注册。

### 4.1 TEditorDataProvide — Editor 模式

```csharp
public class GameEditorDataProvider : TEditorDataProvide<GameEditorDataProvider>
{
    // 通过构造函数注册所有子 Provider
    public GameEditorDataProvider()
    {
        providers = new List<IEditorDataProvider>
        {
            new SkillAssetProvider(),
            new ItemAssetProvider(),
        };
    }

    // 需在子类中显式添加初始化入口
    [InitializeOnLoadMethod]
    [RuntimeInitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        ConfigLoaderManager.Instance.TrySetDataProvider(Instance);
    }
}
```

> `TrySetDataProvider` 是幂等的，已有 DataProvider 时不覆盖，确保 Runtime 优先级高于 Editor。

### 4.2 TAssetExportDataProvider — 基于 ScriptableObject 的导出

当配置数据存储在 ScriptableObject 中并需二进制化时，继承 `TAssetExportDataProvider<TAsset>`：

```csharp
public class SkillAssetProvider : TAssetExportDataProvider<SkillConfigAsset>
{
    protected override bool CheckAssetPath(string path)
        => path.StartsWith("Assets/Config/Skill/");

    protected override void DoExport(SkillConfigAsset asset)
    {
        byte[] data = Serialize(asset);  // 自定义序列化
        string exportPath = AssetExportPath("Skill", asset.name);
        File.WriteAllBytes(exportPath, data);
        OnExportData("Skill", asset.name, data);  // 通知 ConfigLoaderManager 更新缓存
    }

    protected override string AssetExportPath(string type, string name)
        => $"Library/ConfigExport/{type}/{name}.bin";

    protected override string GetAssetPath(string type, string name)
        => SearchFileInFolder("Assets/Config/Skill/", $"{name}.asset");
}
```

**加载时机：**
- `Load` 会比较 Asset 文件与导出文件的修改时间，旧导出自动重新生成。
- Asset 导入时（`OnAssetModify`）自动触发 `DoExport`，无需手动干预。

---

## 五、Entity 配置模式

适用于结构较复杂、需要在编辑器中可视化拼装、不来自 Excel 的实体配置。

### 5.1 定义运行时接口

```csharp
// 运行时实体配置接口
public class HeroEntityConfig : IEntityConfig
{
    public List<IConfigComponent> Components { get; } = new List<IConfigComponent>();
}

// 运行时组件接口
public struct HeroBaseComponent : IConfigComponent
{
    public int MaxHp;
    public float MoveSpeed;
}
```

### 5.2 定义编辑器 ScriptableObject

```csharp
// 实体配置 Asset（自动获得自定义 Inspector）
[CreateAssetMenu(menuName = "Config/Hero")]
public class HeroConfig : TEntityConfig<HeroEntityConfig> { }

// 编辑器组件，T 对应运行时结构
public class HeroBaseConfigComponent : TConfigComponent<HeroBaseComponent> { }
```

### 5.3 在编辑器中编辑

右键 → **Config/Hero** 创建 Asset，Inspector 会自动呈现：

- 顶部：`Enable` 开关 + 所有自身字段（通过 PropertyEditor 反射生成）
- 组件列表：已添加的 `ConfigComponent` 列表，每个可折叠编辑
- **Add Component**：弹出所有 `ConfigComponent` 子类的菜单
- **Export**：调用 `EntityConfig.Export()` 将编辑器数据转换为运行时格式

### 5.4 实现导出逻辑

```csharp
public class HeroConfig : TEntityConfig<HeroEntityConfig>
{
    public override void Export()
    {
        var runtime = new HeroEntityConfig();
        foreach (var component in Components)
        {
            if (component.enabled)
                runtime.Components.Add(component.Export());
        }
        // 序列化 runtime 并写入文件 / 注册到 ConfigLoaderManager
        var bytes = Serialize(runtime);
        File.WriteAllBytes($"Assets/ExportedConfig/Hero/{name}.bin", bytes);
        ConfigLoaderManager.OnDataModify("Hero", name, bytes);
    }
}
```

---

## 六、Attribute 速查表

| Attribute | 作用目标 | 说明 |
|-----------|----------|------|
| `[ExcelSheet("Name")]` | `class`/`struct` | 指定对应 Excel 页签名 |
| `[KeyColumn("ColName")]` | `class`/`struct` | 指定 Dictionary Key 对应的列名 |
| `[ColumnName("ColName")]` | `field` | 覆写字段对应的列名（默认用字段名） |
| `[FieldSeparator('|')]` | `class`/`field` | 指定列表字段或内联结构体字段的分隔符 |
| `[DictionarySeparator(';', ':')]` | `field` | Dictionary 字段：元素分隔符 + 键值分隔符 |
| `[DynimaicList("Col", '|')]` | `field` | 多列映射到一个 List/Array，列名相同、ArrayIndex 递增 |
| `[ConfigIndex]` | `struct` | 标记该结构体支持内联（列名以类型名为前缀） |

---

## 七、常见问题

**Q: Excel 修改后数据没有更新？**
- 确认 `ExcelDataManager.ExcelPath` 已在编辑器启动时正确设置。
- 查看 `Library/ExcelCache/` 目录下是否生成了对应 `*.json` 文件。
- 手动调用 `ExcelDataManager.instance.OnExceleFileChange()` 强制重新导出。

**Q: 运行时 `Get` 返回 default？**
- 检查 `ConfigLoaderManager` 的 `DataProvider` 是否已注册（`[InitializeOnLoadMethod]` 或 `[RuntimeInitializeOnLoadMethod]` 是否生效）。
- 确认 `TypeName` 与注册到 `ConfigLoaderManager` 的 Loader 一致。

**Q: 列表字段导出后长度为 0？**
- 检查是否添加了 `[FieldSeparator]` 或 `[DynimaicList]`。
- 若使用 `ListColumnReader`，列名需以 `.` 结尾来触发 `StructColumnReader`；否则列名需与字段名完全一致。

**Q: DictionaryDataCollector 搜索列表未刷新？**
- 搜索列表 `SearchList` 有懒加载缓存，数据重新读取后 `searchList` 会被置 null，下次访问自动重建。
- 若手动修改了 `items`，需将 `searchList = null` 以使缓存失效。

**Q: 多个 Excel 文件有同名页签会怎样？**
- `ExcelToCache` 会为每个页签生成独立的 `{SheetName}.json`，同名页签后者覆盖前者。建议通过 `IExcelExportFilter` 确保页签名全局唯一。
