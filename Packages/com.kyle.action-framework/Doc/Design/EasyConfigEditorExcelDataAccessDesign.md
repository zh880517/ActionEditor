# EasyConfig Editor Excel 数据访问设计

## 背景和目标

`EasyConfig` 当前已经有两套不同的数据读取链路：

- Runtime 二进制加载链路：由生成代码、`IConfigBytesProvider`、`IConfigSerializer`、`ConfigLoaderManager` 和 `IDataProvider` 负责运行时配置数据来源。
- Editor Excel 缓存链路：由 `ExcelDataManager`、`ExcelToCache` 和 `ExportUtil` 负责把 Excel 导出为 `Library/ExcelCache` 下的 JSON，并在 Editor 中重新读取。

本设计只处理第二条链路：**Editor 工具如何直接读取 Excel JSON 缓存中的配置数据**。它面向编辑器窗口、Inspector、下拉选择器、校验工具等 Editor 工具场景，不参与 Runtime 二进制加载，也不改变 Runtime 配置访问语义。

目标：

- 提供 Editor 专用访问模板类 `EditorListConfig<T>` 和 `EditorDictionaryConfig<T>`。
- 访问数据时复用 `ExportUtil` 从 `Library/ExcelCache` 读取 JSON，不新增 Excel 解析链路。
- 首次访问后把配置类型注册给热刷新派发器，供第三阶段热刷新设计使用。
- 兼容 `EasyConfigExcelLinkedConfigSplitDesign.md` 中新增的关联配置拆分能力。
- 明确 Editor 数据访问和 Runtime 数据加载的边界，避免工具层读取逻辑混入 Runtime 加载链路。

## 术语和边界

| 术语 | 含义 |
| --- | --- |
| Editor 数据访问 | Editor 工具读取 Excel JSON 缓存并访问配置 Collector 的能力 |
| Editor 访问模板类 | Editor 模式下访问 Excel JSON 缓存的 `EditorListConfig<T>` / `EditorDictionaryConfig<T>` |
| Excel 缓存 | `Library/ExcelCache` 下由 `ExcelToCache` 输出的 JSON 文件 |
| 静态 Collector | `ConfigListCollector<T>` / `ConfigDictionaryCollector<TKey, T>` |
| 普通配置 | 继承 `ListConfig<T>` 或 `DictionaryConfig<TKey, T>` 的配置类型 |
| 关联配置 | 继承 `LinkedListConfig<TLinked, TPrimary>` 或 `LinkedDictionaryConfig<TKey, TLinked, TPrimary>` 的配置类型 |
| 主配置 | 关联配置声明的 `TPrimary` 类型 |
| 热刷新注册 | Editor 数据访问在首次读取后登记配置类型，表示该类型需要响应 Excel 缓存变化 |

边界：

- 不修改 Runtime `ListConfig<T>` / `DictionaryConfig<TKey, T>` 的现有访问语义。
- 不调用 Runtime 二进制生成类的 `LoadAll` 作为 Editor 数据读取入口。
- 不改变 `ConfigLoaderManager` / `IDataProvider` 的二进制数据加载职责。
- 不引入新的 Excel 解析逻辑，继续复用 `ExportUtil`、`ColumnReader` 和 `ConvertUtil`。
- 不新增 `.asmdef` 文件。
- Editor 访问能力只位于 Editor 编译范围，不在 Runtime 文件中添加 `UNITY_EDITOR` 访问分支。

## 总体流程

Editor 工具访问配置数据时：

```text
Editor 工具访问 EditorListConfig<T>.Configs
  -> Editor 访问模板类判断 T 是否已经加载
  -> 使用 ExportUtil.Read<T>(ExcelDataManager.CachePath) 读取 Excel JSON 缓存
  -> 如果 T 是关联配置，执行主配置关联
  -> 向 EditorExcelConfigReloadDispatcher 注册 T
  -> 返回 ConfigListCollector<T>.Configs
```

Dictionary 流程类似，只是在读取前需要从 `T` 的继承链推导 `TKey`，再调用 `ExportUtil.Read<TKey, T>(ExcelDataManager.CachePath)`。

这里的 “加载” 只表示 Editor 侧从 JSON 缓存填充静态 Collector，不表示 Runtime 配置已经加载，也不要求 Runtime 二进制文件存在。

## 类型元数据

Editor 数据访问需要识别四类配置：

```text
ConfigKind.List
ConfigKind.Dictionary
ConfigKind.LinkedList
ConfigKind.LinkedDictionary
```

识别规则必须与 `EasyConfigExcelLinkedConfigSplitDesign.md` 保持一致。建议抽出一套可被 Editor 访问、二进制生成和热刷新派发共同复用的类型元数据识别工具：

```text
Type Type
ConfigKind Kind
Type KeyType
Type PrimaryType
string SheetName
```

约束：

- `List` 和 `LinkedList` 使用 `ConfigListCollector<T>`。
- `Dictionary` 和 `LinkedDictionary` 使用 `ConfigDictionaryCollector<TKey, T>`。
- `LinkedDictionary` 的 `PrimaryType` 必须是相同 `TKey` 的 `DictionaryConfig<TKey, TPrimary>`。
- `LinkedList` 的 `PrimaryType` 必须是 `ListConfig<TPrimary>`。
- 类型校验失败时输出 `Debug.LogError`，当前类型不进入 Editor 数据访问。

## EditorListConfig<T>

新增 Editor 模式 List 访问模板类，位于 `EasyConfig/Editor/Excel` 范围：

- `T` 必须是 `List` 或 `LinkedList` 配置类型。
- 第一次访问 `Count`、`Configs`、`Get`、`Find`、`Exists` 时懒加载。
- 懒加载使用 `ExportUtil.Read<T>(ExcelDataManager.CachePath)`。
- 如果 `T` 是 `LinkedList`，读取后按 Index 执行 `Primary` 关联。
- 加载后向热刷新派发器注册对应配置类型。
- 后续访问直接读取 `ConfigListCollector<T>`，不重复读取 JSON。

访问 API 可以尽量靠近 `ListConfig<T>` 的常用方法，方便 Editor 工具迁移，但文档和代码命名必须体现这是 Editor 专用入口。

`ExcelDataManager.CachePath` 当前是私有属性，实现时需要调整为 Editor 程序集内可访问的只读入口，确保 Editor 数据访问和热刷新逻辑使用同一个缓存目录。

## EditorDictionaryConfig<T>

新增 Editor 模式 Dictionary 访问模板类，位于 `EasyConfig/Editor/Excel` 范围：

- `T` 必须是 `Dictionary` 或 `LinkedDictionary` 配置类型。
- 第一次访问 `Configs`、`Get`、`Contains` 时懒加载。
- 懒加载时通过类型元数据取得 `TKey`。
- 懒加载使用 `ExportUtil.Read<TKey, T>(ExcelDataManager.CachePath)`。
- 如果 `T` 是 `LinkedDictionary`，读取后按 Key 执行 `Primary` 关联。
- 加载后向热刷新派发器注册对应配置类型。

由于模板类只有一个泛型参数，`Get` / `Contains` 第一版可以提供以下形态：

- `Get(object key)`：内部校验 key 类型，并转发到 `ConfigDictionaryCollector<TKey, T>`。
- `Contains(object key)`：内部校验 key 类型，并转发到 `ConfigDictionaryCollector<TKey, T>`。
- `Configs`：返回非泛型或反射包装后的只读视图。

如果后续希望完全强类型访问，可以额外增加 `EditorDictionaryConfig<TKey, T>`。第一版优先实现单泛型入口，降低 Editor 工具调用门槛。

## 关联配置访问

关联配置读取后需要恢复 `Primary` 引用：

- `LinkedListConfig<TLinked, TPrimary>` 使用 `ConfigListCollector<TLinked>` 保存关联配置数据。
- `LinkedDictionaryConfig<TKey, TLinked, TPrimary>` 使用 `ConfigDictionaryCollector<TKey, TLinked>` 保存关联配置数据。
- 关联配置从 Excel JSON 缓存读取后，需要恢复每条关联配置到对应主配置对象的引用。
- 关联配置 JSON 读取不应额外序列化 `Primary` 字段。

关联规则与 `EasyConfigExcelLinkedConfigSplitDesign.md` 保持一致：

- `LinkedDictionary` 按相同 Key 从 `ConfigDictionaryCollector<TKey, TPrimary>` 查找主配置。
- `LinkedList` 按相同行索引从 `ConfigListCollector<TPrimary>` 查找主配置。
- 关联失败时输出错误，缺失项的 `Primary` 保持默认值。

Editor 侧不应长期复制一套独立关联规则。优先复用生成器产生的关联逻辑，或抽出与生成器同源的内部关联辅助方法；如果实现阶段需要临时 Editor 辅助方法，也必须复用同一套 `ConfigKind`、`KeyType` 和 `PrimaryType` 元数据。

## 与热刷新派发的协作

Editor 数据访问模板类不直接监听 `FileSystemWatcher`，而是与热刷新派发器协作。

首次访问：

```text
Editor 访问模板类读取 Excel JSON 缓存
  -> 必要时执行关联配置的 Primary 恢复
  -> EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(T))
```

Excel 改变后：

```text
EditorExcelConfigReloadDispatcher.NotifyModify(sheetName)
  -> 对已注册配置类型重新调用 ExportUtil.Read(...)
  -> 如果重新读取的是关联配置，重新执行 Primary 恢复
  -> 覆盖对应静态 Collector
```

如果同一个 Excel 页签同时包含主配置和关联配置，刷新顺序必须保证关联配置恢复 `Primary` 之前，主配置 Collector 已经重新读取。排序规则属于热刷新设计，Editor 数据访问只负责首次读取和注册。

## 与生成代码的关系

`ExcelBinaryCodeGenerator` 仍然负责 Runtime 二进制加载代码。Editor 数据访问不通过生成类的 `LoadAll` 读取 Excel JSON 缓存。

Editor 数据访问可以复用生成链路中的两类能力：

- 配置类型元数据识别结果。
- 关联配置的 `Primary` 恢复规则或生成入口。

约束：

- Runtime 生成文件不引用 Editor 类型。
- Editor 数据访问不主动触发 Runtime 二进制加载。
- 生成器如果额外输出 Editor 注册代码，只注册类型或元数据，不主动读取配置。
- 没有使用生成代码的项目仍可由 Editor 访问模板类在首次访问时按类型推导元数据并注册。

## 生命周期

### 首次访问

```text
Editor 工具访问 EditorListConfig<T> / EditorDictionaryConfig<T>
  -> 元数据校验
  -> 从 Library/ExcelCache 读取 JSON
  -> 填充 ConfigListCollector<T> 或 ConfigDictionaryCollector<TKey, T>
  -> 必要时恢复 Primary
  -> 注册热刷新
```

### 重复访问

```text
Editor 工具再次访问同一类型
  -> 直接读取静态 Collector
```

### Excel 改变

热刷新派发器重新读取已注册类型。Editor 访问模板类不直接监听 Excel 文件变化。

### 清理

Editor 访问模板类可以提供内部清理入口，用于域重载或测试场景清理已加载标记。Collector 清理由现有配置清理逻辑或生成代码负责。

## 风险和取舍

### 与 Runtime API 过于相似

Editor 访问 API 如果完全复刻 Runtime 配置 API，容易让调用方误以为它参与 Runtime 数据加载。

取舍：

- 保留 `Configs`、`Get`、`Contains` 等熟悉访问形态。
- 类型名固定使用 `Editor*Config<T>`，并放在 Editor 编译范围。
- 文档明确其数据来源只来自 Excel JSON 缓存。

### 单泛型 Dictionary 访问

`EditorDictionaryConfig<T>` 无法在编译期暴露 `TKey`。第一版使用类型元数据推导 `TKey`，并在运行时校验 key 类型。

取舍：

- 优点：调用入口简单，适合 Editor 工具快速读取配置。
- 缺点：`Get(object key)` 少了编译期类型检查。

### 关联配置读取顺序

关联配置恢复 `Primary` 前需要主配置 Collector 已经存在。首次访问关联配置时，如果主配置尚未读取，第一版按严格模式输出错误，不自动跨类型加载主配置。

这样可以避免 Editor 数据访问隐式触发一串依赖读取，也保持与 Runtime 分组加载顺序一致。

### 重复注册

首次访问、生成注册入口和域重载可能重复触发注册。热刷新派发器必须按配置类型去重，避免同一类型被重复读取。

## 验证策略

- 检查 Editor 访问类位于 Editor 编译范围。
- 检查读取 Excel JSON 时继续使用 `ExportUtil.Read(...)`。
- 检查 Editor 数据访问不调用 Runtime 生成类 `LoadAll`。
- 检查 Runtime 文件不直接引用 `UnityEditor`。
- 检查 `LinkedList` / `LinkedDictionary` 在 Editor 首次读取后会恢复 `Primary`。
- 检查未新增 `.asmdef` 文件。

## 开发任务

### Editor 访问入口

- [x] 新增 `EditorListConfig<T>`，位于 Editor 编译范围。
- [x] 新增 `EditorDictionaryConfig<T>`，位于 Editor 编译范围。
- [x] `EditorListConfig<T>` 首次访问时懒加载 `ConfigListCollector<T>`。
- [x] `EditorDictionaryConfig<T>` 首次访问时懒加载 `ConfigDictionaryCollector<TKey, T>`。
- [x] 后续访问直接读取 Collector，不重复读取 JSON。
- [x] 访问入口命名和文档明确这是 Editor 专用 API。

### Excel JSON 缓存读取

- [x] 将 `ExcelDataManager.CachePath` 暴露为 Editor 程序集内可访问的只读入口。
- [x] `EditorListConfig<T>` 使用 `ExportUtil.Read<T>(ExcelDataManager.CachePath)`。
- [x] `EditorDictionaryConfig<T>` 使用 `ExportUtil.Read<TKey, T>(ExcelDataManager.CachePath)`。
- [x] 不调用 Runtime 生成类 `LoadAll` 读取 Editor 数据。
- [x] 不依赖 `ConfigLoaderManager` / `IDataProvider`。

### 类型元数据复用

- [x] 抽出或复用关联配置拆分阶段的配置类型元数据识别逻辑。
- [x] `EditorListConfig<T>` 只接受 `List` / `LinkedList`。
- [x] `EditorDictionaryConfig<T>` 只接受 `Dictionary` / `LinkedDictionary`。
- [x] `EditorDictionaryConfig<T>` 通过元数据取得 `TKey`。
- [x] 类型不合法时输出 `Debug.LogError`，并阻止读取。

### Editor 侧 Primary 恢复

- [x] `LinkedList` 读取后按 Index 恢复 `Primary`。
- [x] `LinkedDictionary` 读取后按 Key 恢复 `Primary`。
- [x] 优先复用生成器同源的关联入口或内部辅助方法。
- [x] 主配置 Collector 尚未加载时输出严格错误，不隐式跨类型加载主配置。
- [x] 确认 Editor JSON 读取不会序列化或覆盖 `Primary` 字段。

### 热刷新注册协作

- [x] 首次成功读取后调用 `EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(T))`。
- [x] 重复访问同一类型不重复注册。
- [x] 如果热刷新派发器尚未实现，先预留清晰的内部调用点。

### 验证

- [x] Editor 访问类仅位于 Editor 编译范围。
- [ ] Editor 工具可以从 `Library/ExcelCache` 读取 List 配置。（命令行环境未运行 Unity Editor 实例验证）
- [ ] Editor 工具可以从 `Library/ExcelCache` 读取 Dictionary 配置。（命令行环境未运行 Unity Editor 实例验证）
- [ ] Editor 工具可以读取关联配置并恢复 `Primary`。（命令行环境未运行 Unity Editor 实例验证）
- [x] 未触发 Runtime 二进制加载入口。
