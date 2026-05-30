# EasyConfig Excel 编辑器热刷新设计

## 背景和目标

`EasyConfig` 的 Excel 编辑器链路按开发顺序拆成三层能力：

1. `EasyConfigExcelLinkedConfigSplitDesign.md` 定义关联配置拆分、类型识别和 `Primary` 恢复规则。
2. `EasyConfigEditorExcelDataAccessDesign.md` 定义 Editor 工具如何从 `Library/ExcelCache` 读取 Excel JSON 缓存。
3. 本文档定义 Excel JSON 缓存更新后，如何让已经通过 Editor 数据访问加载过的配置类型自动重新读取缓存。

本文档只设计 Editor 工作流中的热刷新派发机制。它不处理 Runtime 二进制热更新，不调用 Runtime 生成类的 `LoadAll`，也不改变 `ConfigLoaderManager` / `IDataProvider` 的职责。

核心目标：

- Excel 文件变化并重新导出缓存后，通知已注册且已加载的 Editor 配置类型重新读取。
- 热刷新只处理 Editor Excel 缓存访问状态，不修改 Runtime 配置访问语义。
- 与 Editor 数据访问模板类协作，由访问模板类或生成代码注册需要刷新的配置类型。
- 与关联配置拆分协作，保证同一页签内主配置优先刷新，关联配置在刷新后重新恢复 `Primary`。
- 生成代码只在 Editor 编译范围提供注册入口或元数据，Runtime 生成文件不引用 Editor 类型。

## 当前架构理解

### Excel 缓存导出

`ExcelDataManager` 是 Editor 层的 `ScriptableSingleton`：

- `ExcelPath` 指向本地 Excel 根目录。
- `FileSystemWatcher` 监听 `*.xlsx` 的 `LastWrite`。
- 文件变化后通过 `EditorApplication.delayCall` 延迟执行导出。
- `ExcelToCache.Export` 将 Excel 内容重新导出到 `Library/ExcelCache`。
- 导出阶段会把内容发生变化的页签名加入 `modifySheets`。
- `UpdateByModify` 当前只刷新已创建的 `ExcelDataCollector`。

热刷新派发必须发生在 `ExcelToCache.Export` 完成之后，确保重新读取时看到的是最新 JSON 缓存。

### Editor 数据访问

Editor 工具不应直接依赖 Runtime 二进制加载。`EasyConfigEditorExcelDataAccessDesign.md` 已经定义：

- `EditorListConfig<T>` 懒加载 `ConfigListCollector<T>`。
- `EditorDictionaryConfig<T>` 懒加载 `ConfigDictionaryCollector<TKey, T>`。
- 懒加载时复用 `ExportUtil.Read(...)` 读取 `Library/ExcelCache`。
- 首次访问后向热刷新派发器注册对应配置类型。
- 关联配置首次读取后需要恢复 `Primary`。

因此本文档不再定义 Editor 数据访问模板类，只要求热刷新派发机制能被这些模板类调用。

### 关联配置拆分

`EasyConfigExcelLinkedConfigSplitDesign.md` 已经定义四类配置：

- `List`
- `Dictionary`
- `LinkedList`
- `LinkedDictionary`

并定义了关联配置到主配置的 `Primary` 恢复规则。热刷新只复用这些类型识别结果和关联入口，不复制另一套长期独立的关联算法。

## 术语和边界

| 术语 | 含义 |
| --- | --- |
| Excel 缓存 | `Library/ExcelCache` 下由 `ExcelToCache` 输出的 JSON 文件 |
| 页签名 | `ExcelSheetAttribute` 声明的 Excel sheet 名称 |
| 配置类型 | 继承配置模板类的实际类型，例如 `SkillCoreConfig`、`SkillDisplayConfig` |
| 普通配置 | `List` 或 `Dictionary` 类型配置 |
| 关联配置 | `LinkedList` 或 `LinkedDictionary` 类型配置 |
| 主配置 | 关联配置声明的 `PrimaryType` |
| 静态 Collector | `ConfigListCollector<T>` / `ConfigDictionaryCollector<TKey, T>` |
| 热刷新派发器 | 保存已注册配置类型，并按页签派发重新读取的 Editor 类 |
| 刷新执行器 | 针对单个配置类型执行 `ExportUtil.Read(...)` 和必要 `Primary` 恢复的内部对象 |

边界：

- 不修改 `ListConfig<T>` / `DictionaryConfig<TKey, T>` 的 Runtime 访问语义。
- 不定义 `EditorListConfig<T>` / `EditorDictionaryConfig<T>`，它们属于 Editor 数据访问设计。
- 不定义关联配置模板类和关联算法，它们属于关联配置拆分设计。
- 不引入新的 Excel 解析逻辑，继续复用 `ExportUtil`、`ColumnReader` 和 `ConvertUtil`。
- 不改变 `ConfigLoaderManager` / `IDataProvider` 的二进制数据加载职责。
- 不新增 `.asmdef` 文件。
- Runtime 文件不使用 `UNITY_EDITOR` 宏引用热刷新逻辑。

## 方案概览

新增 Editor 侧热刷新派发机制：

1. Editor 数据访问模板类或生成代码把配置类型注册到热刷新派发器。
2. 派发器按 `ExcelSheetAttribute` 维护页签到配置类型的映射。
3. `ExcelDataManager.UpdateByModify()` 在缓存导出后把变化页签通知派发器。
4. 派发器只刷新已注册的配置类型。
5. 如果刷新类型是关联配置，刷新后调用与生成代码同源的 `Primary` 恢复入口。

整体流向：

```text
Excel 文件变化
  -> ExcelDataManager.OnExceleFileChange()
  -> ExcelToCache.Export() 写入 Library/ExcelCache
  -> ExcelDataManager.UpdateByModify()
  -> EditorExcelConfigReloadDispatcher.NotifyModify(sheetName)
  -> 按页签找出已注册配置类型
  -> 按普通配置 / 关联配置顺序重新读取 Collector
  -> 关联配置重新恢复 Primary
```

## 关键设计

### EditorExcelConfigReloadDispatcher

在 Editor 程序集中新增热刷新派发类，建议提供以下能力：

```text
RegisterConfigType(Type configType)
UnregisterConfigType(Type configType)
NotifyModify(string sheetName)
Clear()
```

职责：

- 接收 Editor 数据访问模板类或生成代码注册的配置类型。
- 从类型元数据中取得页签名、配置分类、Key 类型和 `PrimaryType`。
- 按页签保存已注册配置类型。
- 收到页签变化通知后，只刷新对应页签下已注册类型。
- 对重复注册去重，避免域重载、首次访问和生成注册入口重复执行时产生多次读取。

派发器只位于 Editor 编译范围，可以依赖 `ExportUtil`、`ExcelDataManager.CachePath`、类型识别工具和关联入口。

### 类型元数据来源

热刷新需要的类型元数据应与关联配置拆分和 Editor 数据访问保持一致：

```text
Type Type
ConfigKind Kind
Type KeyType
Type PrimaryType
string SheetName
```

元数据可以来自两种路径：

- 生成代码把 `ExcelBinaryTypeCollector` 已经识别出的类型数据注册给派发器。
- Editor 数据访问模板类首次访问时，通过同一套类型识别辅助逻辑推导当前类型。

关键约束是：热刷新、Editor JSON 访问和 Runtime 二进制生成必须使用同一种配置分类规则，避免某个类型在不同链路中被判断成不同种类。

### 刷新执行器

派发器内部为每个配置类型创建刷新执行器。执行器负责：

- `List` 和 `LinkedList` 调用 `ExportUtil.Read<T>(ExcelDataManager.CachePath)`。
- `Dictionary` 和 `LinkedDictionary` 调用 `ExportUtil.Read<TKey, T>(ExcelDataManager.CachePath)`。
- 读取失败时输出错误，并保留 `ExportUtil` 当前的异常或日志语义。
- 关联配置读取完成后触发 `Primary` 恢复。

刷新执行器是派发器的内部实现细节，不要求业务侧直接持有监听器对象。

### 同页签刷新顺序

同一个 Excel 页签可能同时被主配置和关联配置读取。热刷新必须保证：

1. 先刷新普通配置，也就是 `List` / `Dictionary`。
2. 再刷新关联配置，也就是 `LinkedList` / `LinkedDictionary`。
3. 关联配置 Collector 更新后重新恢复 `Primary`。

如果主配置和关联配置位于不同页签，派发器只处理当前变化页签对应的类型。关联阶段如果找不到主配置数据，沿用关联配置拆分设计中的严格模式输出错误。

### Primary 恢复入口

热刷新不应长期维护一套独立关联实现。优先方案是由 `ExcelBinaryCodeGenerator` 生成可复用的关联入口，或抽出与生成器同源的内部辅助方法：

- Runtime `LoadAll` 在加载关联配置分组后调用关联入口。
- Editor 首次读取关联配置后调用同一关联入口。
- Editor 热刷新关联配置后调用同一关联入口。

如果实现阶段需要先落一个 Editor 内部辅助方法，也必须复用与生成器相同的 `ConfigKind`、`KeyType` 和 `PrimaryType` 元数据，并在后续收敛到同源关联入口。

## 与 Editor 数据访问设计的关系

Editor 数据访问模板类负责“何时首次读取”和“如何提供访问 API”：

```text
Editor 工具访问 EditorListConfig<T>.Configs
  -> Editor 数据访问模板类判断 T 尚未加载
  -> ExportUtil.Read<T>(CachePath)
  -> 必要时恢复 Primary
  -> EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(T))
  -> 返回 Collector 数据
```

热刷新派发器负责“Excel 缓存变化后如何让已注册类型重新读取”：

```text
EditorExcelConfigReloadDispatcher.NotifyModify(sheetName)
  -> 查找已注册配置类型
  -> ExportUtil.Read(...) 覆盖 Collector
  -> 必要时恢复 Primary
```

两者通过配置类型注册协作，不共享 UI 状态，也不直接监听彼此的生命周期。

## 与生成代码的关系

`ExcelBinaryCodeGenerator` 的 Runtime 生成文件继续只负责：

- `LoadAll(IConfigBytesProvider provider, IConfigSerializer serializer)`
- `Clear()`
- 当前分组需要时执行关联配置的 `Primary` 恢复

为支持 Editor 热刷新，生成器可以额外输出 Editor 编译范围内的注册入口，例如：

```csharp
public static void RegisterEditorExcelReloadTypes()
{
    EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(SkillCoreConfig));
    EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(SkillDisplayConfig));
}
```

约束：

- Editor 注册文件必须位于 Editor 编译范围。
- Runtime 生成文件不引用 `UnityEditor`，也不引用热刷新派发类。
- 生成注册入口只注册元数据或配置类型，不主动读取配置。
- 没有使用生成代码的项目仍可由 Editor 数据访问模板类在首次访问时按类型注册。

## ExcelDataManager 派发时机

`ExcelDataManager.OnExceleFileChange()` 在 `ExcelToCache.Export` 完成后应调用 `UpdateByModify()`，形成一次完整流程：

```text
文件变化
  -> 导出 JSON 缓存
  -> 刷新 ExcelDataCollector
  -> 派发 Editor 配置热刷新
  -> 清理 modifySheets
```

`UpdateByModify()` 中的建议顺序：

1. 遍历 `modifySheets`，刷新已有 `ExcelDataCollector`。
2. 遍历 `modifySheets`，调用 `EditorExcelConfigReloadDispatcher.NotifyModify(sheetName)`。
3. 清理 `modifySheets`。

派发器内部负责对同一页签下的配置类型排序，`ExcelDataManager` 不需要理解主配置和关联配置关系。

## 生命周期

### 首次访问

```text
Editor 工具首次访问配置数据
  -> Editor 数据访问模板类读取 Excel JSON 缓存
  -> 必要时恢复 Primary
  -> 注册配置类型到热刷新派发器
```

### Excel 改变

```text
FileSystemWatcher 发现 xlsx 变化
  -> EditorApplication.delayCall
  -> ExcelToCache.Export
  -> 收集发生内容变化的 sheetName
  -> UpdateByModify
  -> EditorExcelConfigReloadDispatcher.NotifyModify(sheetName)
  -> 已注册类型重新读取对应 Collector
```

### 清理

热刷新派发器提供 `Clear()` 作为兜底清理入口，用于域重载前、Editor 关闭或业务侧明确清理。

生成代码的 `Clear()` 仍只负责清理 Collector，不强制清理热刷新注册表。是否调用派发器 `Clear()` 由 Editor 初始化和生命周期管理决定。

## 任务拆分

1. Editor 增加 `EditorExcelConfigReloadDispatcher`。
2. 抽出或复用与 `ExcelBinaryTypeCollector` 一致的配置类型元数据识别逻辑。
3. 增加 `List` / `Dictionary` / `LinkedList` / `LinkedDictionary` 的刷新执行器。
4. 将 `ExcelDataManager.CachePath` 暴露为 Editor 程序集内可访问的只读入口。
5. 扩展 `ExcelDataManager.UpdateByModify()`，在 JSON 重新导出后派发页签变更。
6. 扩展 `ExcelBinaryCodeGenerator`，生成 Editor 侧热刷新注册入口或元数据注册代码。
7. 提供关联配置 `Primary` 恢复的可复用入口，供 Runtime 加载、Editor 首次读取和 Editor 热刷新共同调用。
8. 更新 `Doc/EasyConfig.md`，补充 Editor Excel 数据访问和热刷新协作方式。

## 兼容性和迁移

- 已有 `ListConfig<T>` / `DictionaryConfig<TKey, T>` Runtime 调用不需要修改。
- 已有二进制加载流程不受影响。
- 已有 `ExcelDataCollector` 刷新逻辑继续保留。
- 业务侧如果不使用 Editor 数据访问模板类或生成注册入口，则不会自动热刷新对应配置类型。
- 如果项目未使用生成代码，首次访问注册仍可覆盖常见 Editor 工具场景。

## 风险和取舍

### 重复注册

域重载、生成注册入口和首次访问都可能注册同一个配置类型。派发器必须按 `Type` 去重。

### 关联入口复用

`Primary` 恢复如果在 Runtime 生成代码和 Editor 热刷新中各写一套，长期容易出现行为差异。第一版可以用内部辅助方法推进实现，但最终应收敛到同源类型识别和同源关联入口。

### 跨组主配置和关联配置

主配置和关联配置可以位于不同 `ConfigGroupAttribute` 分组。热刷新不负责跨组自动加载顺序；当关联配置刷新并恢复 `Primary` 时主配置 Collector 尚未加载，按严格模式输出错误。

### 多来源同页签

沿用 `ExportUtil` 当前行为：

- `MultiFile=true` 读取全部匹配 JSON。
- `MultiFile=false` 发现多个来源时只读取第一个并输出警告。

本文档不改变多来源读取规则。

## 验证策略

### 静态验证

- 检查 Runtime 程序集不直接引用 `UnityEditor`。
- 检查热刷新注册代码位于 Editor 编译范围。
- 检查热刷新、Editor 数据访问和二进制生成使用一致的配置类型分类。
- 检查未新增 `.asmdef` 文件。

### 行为验证

- 修改 Excel 后，`ExcelToCache.Export` 完成再触发热刷新派发。
- 未注册配置类型不会被热刷新读取。
- 已注册 `List` / `Dictionary` 配置在页签变化后重新读取 Collector。
- 同页签同时包含主配置和关联配置时，主配置先刷新，关联配置后刷新并重新恢复 `Primary`。
- 重复调用注册入口不会导致同一配置类型被重复刷新。
