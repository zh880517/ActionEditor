# Common 基础库

框架的共享基础层，分为两个程序集：`Action.Common.Runtime`（全平台）和 `Action.Common.Editor`（仅 Editor）。

---

## Runtime 模块

| 模块 | 简介 | 文档 |
|------|------|------|
| **Attribute** | 驱动 PropertyEditor 渲染与运行时行为的公共标记属性，含显示控制、字段级编辑器扩展、序列化 GUID 等 | [Attribute.md](Attribute.md) |
| **Serialization** | 基于 JSON + GUID 的多态对象序列化方案（`TypeSerializerHelper` + `SerializationData`），用于编辑器资产的跨重构稳定存储 | [Serialization.md](Serialization.md) |
| **DataVisit** | 基于 Attribute 和代码生成的高性能零反射二进制序列化，支持 SevenBit / RawBit 双协议 | [DataVisit.md](DataVisit.md) |
| **StructSequence** | 单线程非托管内存结构体队列，用于高频生产者/消费者消息传递 | [StructSequence.md](StructSequence.md) |
| **Recycle** | 轻量泛型对象池（`RecyleablePool<T>`），减少高频对象的 GC 压力 | [Recycle.md](Recycle.md) |
| **Util** | 运行时工具集：`FileUtil`、`MD5Util`、`NetJsonUtil` | — |
| **CollectableScriptableObject** | `ScriptableObject` 的空基类标记，供 `ScriptObjectCollector` 统一追踪 | — |

---

## Editor 模块

| 模块 | 简介 | 文档 |
|------|------|------|
| **PropertyEditor** | 反射驱动的通用属性 UI，基于 UIToolkit 自动为任意可序列化类型生成编辑界面，支持 25+ 内置类型及自定义扩展 | [PropertyEditor.md](PropertyEditor.md) |
| **Timeline** | 可复用的 UIToolkit 时间轴控件（`TimelineView` / `TrackView` / `ClipView`），ActionLine 编辑器的 UI 基础 | [Timeline.md](Timeline.md) |
| **TypeCollector** | 三个反射类型收集器（`TypeCollector` / `AttributeTagTypeCollector` / `TypeWithAttributeCollector`），惰性缓存，框架扩展点的发现机制基础 | [TypeCollector.md](TypeCollector.md) |
| **CSharpCodeWriter** | 流式 C# 源码生成工具，自动管理缩进与代码块结构，供 DataVisit、FlowGraph 等代码生成器使用 | [CSharpCodeWriter.md](CSharpCodeWriter.md) |
| **ScriptObjectCollector** | `ScriptableSingleton`，追踪和缓存磁盘 `.asset` 文件，监听资产增删变化并触发事件 | [ScriptObjectCollector.md](ScriptObjectCollector.md) |
| **DataVisit 代码生成** | DataVisit 的编辑器端代码生成入口，扫描 Catalog 类型并输出序列化代码 | [DataVisit.md](DataVisit.md) |
| **StructSequence 代码生成** | StructSequence 的编辑器端代码生成入口，扫描 `StructSequenceCatalogAttribute` 标记的 struct，自动生成读写委托注册代码（`Tools/StructSequence/Generate All`） | [StructSequence.md](StructSequence.md) |
| **VisualElement 组件** | 通用小控件：`IconButton`、`MouseCursorRect`、`PlayButtonsView`、`TypeSelectWindow` | — |
| **Editor Utils** | 编辑器工具集：`GeneratorUtils`、`HandlesUtil`、`MonoScriptUtil`、`VisualElementUtil` | — |
