# EasyConfig Excel 开发 TODO List

## 开发顺序

本 TODO 只保留按设计文档划分的顶层任务。每份设计文档是一项开发任务，具体子任务维护在对应文档的“开发任务”章节中。

执行顺序：

1. `EasyConfigExcelLinkedConfigSplitDesign.md`
2. `EasyConfigEditorExcelDataAccessDesign.md`
3. `EasyConfigExcelHotReloadDesign.md`
4. `Doc/EasyConfig.md`

执行原则：

- 先完成关联配置拆分，再做 Editor 数据访问，最后接入热刷新。
- Editor 数据访问只读取 `Library/ExcelCache` 下的 Excel JSON 缓存，不调用 Runtime 生成类 `LoadAll`。
- 热刷新只刷新已经通过 Editor 数据访问注册过的配置类型，不处理 Runtime 二进制热更新。
- 不新增 `.asmdef` 文件。
- 修改或新增模块功能后，同步更新 `Doc/EasyConfig.md`。
- 代码注释和仓库文档使用中文。

## 文档任务

- [ ] `EasyConfigExcelLinkedConfigSplitDesign.md`：实现关联配置拆分、类型识别、二进制导出和关联恢复。
- [ ] `EasyConfigEditorExcelDataAccessDesign.md`：实现 Editor Excel JSON 缓存访问入口和首次读取注册。
- [ ] `EasyConfigExcelHotReloadDesign.md`：实现 Editor Excel 缓存热刷新派发和刷新顺序。
- [ ] `Doc/EasyConfig.md`：补充关联配置、Editor 数据访问和热刷新用法。

## 收尾检查

- [ ] 检查 `Doc/Readme.md` 是否需要增加设计文档入口。
- [ ] 检查所有新增注释和文档字符串使用中文。
- [ ] 检查没有新增 `.asmdef` 文件。
- [ ] 检查 Markdown 使用 LF line endings。
- [ ] 记录无法通过命令行验证的 Unity Editor 验证项。
