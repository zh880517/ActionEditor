# EasyConfig Excel 开发 TODO List

## 开发顺序

本 TODO 按以下设计文档顺序执行：

1. `EasyConfigExcelLinkedConfigSplitDesign.md`
2. `EasyConfigEditorExcelDataAccessDesign.md`
3. `EasyConfigExcelHotReloadDesign.md`

执行原则：

- 先完成关联配置拆分，再做 Editor 数据访问，最后接入热刷新。
- Editor 数据访问只读取 `Library/ExcelCache` 下的 Excel JSON 缓存，不调用 Runtime 生成类 `LoadAll`。
- 热刷新只刷新已经通过 Editor 数据访问注册过的配置类型，不处理 Runtime 二进制热更新。
- 不新增 `.asmdef` 文件。
- 修改或新增模块功能后，同步更新 `Doc/EasyConfig.md`。

## 阶段一：Excel 关联配置拆分

设计文档：`EasyConfigExcelLinkedConfigSplitDesign.md`

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

### 阶段一验证

- [ ] Unity 编译通过。
- [ ] 普通 `ListConfig<T>` / `DictionaryConfig<TKey, T>` 原有导出和加载不受影响。
- [ ] `LinkedList` / `LinkedDictionary` 可以生成独立二进制文件。
- [ ] 加载关联配置分组后，`Primary` 引用恢复正确。
- [ ] 主配置缺失时有明确错误日志。

## 阶段二：Editor Excel 数据访问

设计文档：`EasyConfigEditorExcelDataAccessDesign.md`

### Editor 访问入口

- [ ] 新增 `EditorListConfig<T>`，位于 Editor 编译范围。
- [ ] 新增 `EditorDictionaryConfig<T>`，位于 Editor 编译范围。
- [ ] `EditorListConfig<T>` 首次访问时懒加载 `ConfigListCollector<T>`。
- [ ] `EditorDictionaryConfig<T>` 首次访问时懒加载 `ConfigDictionaryCollector<TKey, T>`。
- [ ] 后续访问直接读取 Collector，不重复读取 JSON。
- [ ] 访问入口命名和文档明确这是 Editor 专用 API。

### Excel JSON 缓存读取

- [ ] 将 `ExcelDataManager.CachePath` 暴露为 Editor 程序集内可访问的只读入口。
- [ ] `EditorListConfig<T>` 使用 `ExportUtil.Read<T>(ExcelDataManager.CachePath)`。
- [ ] `EditorDictionaryConfig<T>` 使用 `ExportUtil.Read<TKey, T>(ExcelDataManager.CachePath)`。
- [ ] 不调用 Runtime 生成类 `LoadAll` 读取 Editor 数据。
- [ ] 不依赖 `ConfigLoaderManager` / `IDataProvider`。

### 类型元数据复用

- [ ] 抽出或复用阶段一的配置类型元数据识别逻辑。
- [ ] `EditorListConfig<T>` 只接受 `List` / `LinkedList`。
- [ ] `EditorDictionaryConfig<T>` 只接受 `Dictionary` / `LinkedDictionary`。
- [ ] `EditorDictionaryConfig<T>` 通过元数据取得 `TKey`。
- [ ] 类型不合法时输出 `Debug.LogError`，并阻止读取。

### Editor 侧 Primary 恢复

- [ ] `LinkedList` 读取后按 Index 恢复 `Primary`。
- [ ] `LinkedDictionary` 读取后按 Key 恢复 `Primary`。
- [ ] 优先复用生成器同源的关联入口或内部辅助方法。
- [ ] 主配置 Collector 尚未加载时输出严格错误，不隐式跨类型加载主配置。
- [ ] 确认 Editor JSON 读取不会序列化或覆盖 `Primary` 字段。

### 热刷新注册协作

- [ ] 首次成功读取后调用 `EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(T))`。
- [ ] 重复访问同一类型不重复注册。
- [ ] 如果热刷新派发器尚未实现，先预留清晰的内部调用点。

### 阶段二验证

- [ ] Editor 访问类仅位于 Editor 编译范围。
- [ ] Editor 工具可以从 `Library/ExcelCache` 读取 List 配置。
- [ ] Editor 工具可以从 `Library/ExcelCache` 读取 Dictionary 配置。
- [ ] Editor 工具可以读取关联配置并恢复 `Primary`。
- [ ] 未触发 Runtime 二进制加载入口。

## 阶段三：Editor Excel 热刷新

设计文档：`EasyConfigExcelHotReloadDesign.md`

### 热刷新派发器

- [ ] 新增 `EditorExcelConfigReloadDispatcher`，位于 Editor 编译范围。
- [ ] 实现 `RegisterConfigType(Type configType)`。
- [ ] 实现 `UnregisterConfigType(Type configType)`。
- [ ] 实现 `NotifyModify(string sheetName)`。
- [ ] 实现 `Clear()`。
- [ ] 注册时按 `Type` 去重。
- [ ] 注册时按 `ExcelSheetAttribute` 建立页签到配置类型的映射。

### 刷新执行器

- [ ] 为 `List` 创建刷新执行器，调用 `ExportUtil.Read<T>(CachePath)`。
- [ ] 为 `LinkedList` 创建刷新执行器，调用 `ExportUtil.Read<T>(CachePath)` 后恢复 `Primary`。
- [ ] 为 `Dictionary` 创建刷新执行器，调用 `ExportUtil.Read<TKey, T>(CachePath)`。
- [ ] 为 `LinkedDictionary` 创建刷新执行器，调用 `ExportUtil.Read<TKey, T>(CachePath)` 后恢复 `Primary`。
- [ ] 刷新失败时保留 `ExportUtil` 当前错误语义，并输出足够定位类型的信息。

### 刷新顺序

- [ ] 同一页签内先刷新 `List` / `Dictionary`。
- [ ] 同一页签内后刷新 `LinkedList` / `LinkedDictionary`。
- [ ] 关联配置刷新后重新恢复 `Primary`。
- [ ] 跨页签主配置未加载时按严格模式输出错误。

### ExcelDataManager 接入

- [ ] `ExcelToCache.Export` 完成后再触发热刷新派发。
- [ ] 扩展 `ExcelDataManager.UpdateByModify()`。
- [ ] 先刷新已有 `ExcelDataCollector`。
- [ ] 再对每个变化页签调用 `EditorExcelConfigReloadDispatcher.NotifyModify(sheetName)`。
- [ ] 最后清理 `modifySheets`。
- [ ] `ExcelDataManager` 不理解主配置和关联配置排序，排序由派发器负责。

### 生成代码协作

- [ ] 评估是否由 `ExcelBinaryCodeGenerator` 输出 Editor 注册入口。
- [ ] Editor 注册入口只注册类型或元数据，不主动读取配置。
- [ ] Runtime 生成文件不引用 `UnityEditor`。
- [ ] 没有生成注册入口时，Editor 访问模板类首次访问注册仍能工作。

### 阶段三验证

- [ ] 修改 Excel 后，缓存导出完成再触发派发。
- [ ] 未注册配置类型不会被热刷新读取。
- [ ] 已注册普通配置在页签变化后重新读取 Collector。
- [ ] 已注册关联配置在页签变化后重新读取 Collector 并恢复 `Primary`。
- [ ] 重复注册不会导致同一类型被重复刷新。
- [ ] Runtime 二进制加载流程不受影响。

## 文档和收尾

- [ ] 更新 `Doc/EasyConfig.md`，补充关联配置、Editor 数据访问和热刷新用法。
- [ ] 检查 `Doc/Readme.md` 是否需要增加设计文档入口。
- [ ] 检查所有新增注释和文档字符串使用中文。
- [ ] 检查没有新增 `.asmdef` 文件。
- [ ] 检查 Markdown 使用 LF line endings。
- [ ] 记录无法通过命令行验证的 Unity Editor 验证项。
