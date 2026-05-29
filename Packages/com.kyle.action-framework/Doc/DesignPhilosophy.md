# Action Framework 架构设计指南（AI Agent 版）

本文供后续 AI Agent 在为 Action Framework 设计模块架构、撰写技术方案或扩展现有系统时使用。

它不是模块使用手册，而是一套从 `EasyConfig`、`ActionLine`、`ECS`、`FlowGraph` 与 `Common` 中提炼出的**架构设计思路、取舍原则和文档写作框架**。Agent 在输出设计文档前，应先用本文校准方向，避免写出与项目风格相冲突的方案。

---

## 最高原则

Action Framework 的架构核心是：

**把复杂性集中在 Editor 侧发现、编辑、校验和生成，把 Runtime 侧压缩成明确的数据结构、泛型入口、生命周期调度和缓存访问。**

Agent 设计任何新模块或改造方案时，都应优先判断：

- 哪些复杂性只属于编辑、预览、导出、生成或调试？
- 哪些数据和逻辑必须进入 Runtime？
- 能否用 Attribute、类型扫描或代码生成减少手动注册？
- 能否让 Runtime 避免反射、避免松散对象图、避免隐式全局状态？
- 能否复用 `Common` 里的现有机制，而不是重新发明一套基础设施？

推荐的整体路径是：

```text
业务类型 / 数据结构 / Attribute 声明
    ↓
Editor 扫描、编辑、校验、导出或生成代码
    ↓
Runtime 使用导出数据、静态泛型入口、Context 和 Collector 执行
```

---

## Agent 设计前的阅读顺序

当用户要求设计新功能或模块架构时，Agent 应按以下顺序阅读项目：

1. 先读对应模块文档。
   - `Doc/EasyConfig.md`
   - `Doc/ActionLine.md`
   - `Doc/ECS.md`
   - `Doc/FlowGraph.md`
   - `Doc/Common/Readme.md`

2. 再读相关源码入口。
   - 数据资产：`ActionLineAsset`、`FlowGraph`、`FlowNode`、`EntityConfig` 等。
   - Runtime Context：`FlowGraphRuntimeContext`、`ConfigLoaderManager`、`LiteECS.Context`、`ViewContext`。
   - 类型发现：`TypeCollector`、`TypeWithAttributeCollector`、`AttributeTagTypeCollector`。
   - UI 基础：`PropertyElementFactory`、`TimelineView`、自定义 `EventBase<T>` 事件。
   - 代码生成：`FlowCodeGenContext`、`ECSGenerator`、`CSharpCodeWriter`、`GeneratorUtils`。

3. 最后再判断新增设计属于哪一层。
   - Editor-only 工具。
   - Editor + Runtime 双层模块。
   - Runtime-only 基础能力。
   - Common 共享基础设施。

不要先凭空设计抽象。这个项目的架构风格非常依赖既有模块边界、Attribute 扩展点、生成器和 Editor/Runtime 分离。

---

## 架构设计时的思考框架

### 1. 先拆 Editor 与 Runtime

设计文档必须明确区分：

| 层级 | 应承担的职责 | 不应承担的职责 |
| --- | --- | --- |
| Editor | 可视化编辑、类型扫描、Asset 监听、预览、校验、导出、代码生成、调试辅助 | 游戏运行时热路径、构建包必须依赖的 UnityEditor API |
| Runtime | 执行数据结构、生命周期、缓存、加载、泛型 API、Context 调度 | AssetDatabase 扫描、程序集反射扫描、编辑器窗口、Inspector 逻辑 |
| Common | 跨模块复用的类型扫描、属性 UI、事件、代码生成、序列化、对象池 | 某个业务模块专属的领域概念 |

如果一个设计把 Editor 的便利直接塞进 Runtime，通常应视为架构风险。

### 2. 先找声明式扩展点

这个项目偏好让业务侧“声明意图”，由框架发现能力。

设计新扩展时，优先考虑：

- 用 Attribute 表达菜单路径、显示名、类型归属、字段映射或行为标签。
- 用接口表达能力类别，例如节点类型、组件类型、配置类型。
- 用泛型基类表达类型安全的扩展入口。
- 用 Provider 表达少数需要替换的全局策略。

只有当扩展行为必须运行时动态变化，才考虑运行时注册表。

### 3. 把编辑资产与运行时数据分开

架构文档中应明确两类模型：

- **编辑模型**：适合 `ScriptableObject`、Inspector、节点图、时间轴、预览、Undo、注释、折叠状态和视口状态。
- **运行模型**：适合 ID、数组、字典、Collector、RuntimeData、Context、缓存和序列化数据。

设计时要说明：

- 编辑模型如何创建和维护？
- 何时导出到运行模型？
- 导出是否增量、缓存或手动触发？
- 运行模型是否可脱离 Editor 使用？
- Debug 信息如何从 Runtime 映射回编辑模型？

参考：

- `FlowGraph` 从节点图导出 `FlowGraphRuntimeData`。
- `ActionLine` 从 `ActionLineAsset` 导出 `ActionClipData` 视图。
- `EasyConfig` 从 Excel 或 Entity 配置导出 Collector / 二进制数据。
- `ECS` 从组件类型生成组件 ID、Context 和 Reset 代码。

### 4. 高频路径优先生成代码

如果设计涉及运行时高频访问，不要默认使用反射。

优先方案：

- Editor 反射扫描类型。
- 生成静态 ID、初始化代码、Reset 代码或 Executor 基类。
- 运行时通过泛型、数组索引、字典缓存或 Collector 访问。

需要在设计文档中写清楚：

- 哪些文件由生成器产生？
- 哪些文件允许业务手写？
- 生成器是否覆盖已有文件？
- 新增类型后是否必须重新生成？
- 生成失败时如何诊断？

生成器应遵循本项目惯例：自动生成文件和手写业务文件分离，优先使用 `partial`，避免覆盖业务逻辑。

### 5. 用 Context 收束生命周期

框架倾向用 Context 管理一个系统的世界状态。

设计新模块时，Agent 应考虑是否需要一个 Context，并说明其职责：

- 当前目标数据或运行数据。
- 缓存与临时状态。
- 生命周期入口，例如 `Start/Update/Stop`、`OnInitialize/OnCleanup`。
- 资源上下文，例如预览对象、加载器、DataProvider。
- 调试上下文或运行时到编辑器的映射。

参考：

- `ActionLineEditorContext` 管理当前时间轴编辑状态。
- `ActionLinePreviewContext` 管理预览帧和 Clip 模拟器生命周期。
- `FlowGraphRuntimeContext` 管理当前节点、变量表、数据节点缓存和调试器。
- `ViewContext` 管理实体、组件 Collector、Static 组件和 Reactive 版本号。
- `ConfigLoaderManager` 管理 DataProvider、Loader 和缓存更新。

### 6. UI 交互使用事件冒泡

设计 UIToolkit 相关功能时，应遵循：

- 子 `VisualElement` 只表达发生了什么。
- 父级 Context 或 View 负责业务决策。
- 交互通过专用 `EventBase<T>` 事件冒泡。
- 不在控件构造函数里传递大量 `Action` 或业务委托。

设计文档中应列出新增事件：

| 事件 | 触发者 | 载荷 | 处理者 | 用途 |
| --- | --- | --- | --- | --- |

如果没有新增事件，也应说明复用了哪个已有事件。

### 7. Runtime API 要小而稳定

运行时 API 应以少量稳定入口暴露能力，而不是暴露内部结构。

好的形态：

- `ListConfig<T>.Get`
- `DictionaryConfig<TKey,T>.Get`
- `Entity<IContext>.Add/Get/Remove`
- `ViewEntity.Modify<T>`
- `FlowGraphRuntimeContext.SetOutputValue`
- `FlowNodeExecutor<T>.Executor`

设计文档应明确：

- 业务侧最小调用方式是什么？
- 哪些内部集合只读暴露？
- 哪些状态由 Context 管理，业务侧不能直接改？
- 出错时返回 default、抛异常，还是记录日志？

---

## 从现有模块提炼出的设计模板

### 模板 A：可视化编辑器模块

适用于时间轴、节点图、实体配置、编辑器面板等工具。

推荐结构：

```text
Module/
├── Runtime/             # 若运行时需要使用导出数据
│   ├── Data/
│   ├── Loader/
│   └── Context/
└── Editor/
    ├── Asset/
    ├── ElementView/
    ├── Context/
    ├── Window/
    ├── Event/
    └── CodeGen/         # 如需要
```

设计文档必须说明：

- 编辑资产是什么。
- 编辑器窗口或 Inspector 如何打开。
- 子控件如何通过事件通信。
- Undo 和 Dirty 如何处理。
- 是否需要预览上下文。
- 如何导出 RuntimeData。
- 扩展点使用 Attribute、Provider 还是继承基类。

参考模块：`ActionLine`、`FlowGraph`、`EasyConfig.Entity`。

### 模板 B：运行时执行系统

适用于图执行、ECS、配置加载、行为调度等。

推荐结构：

```text
Runtime/
├── Data/
├── Context/
├── Component 或 Node/
├── Executor 或 System/
├── Loader 或 Provider/
└── Debug/               # 可选，最好用编译宏隔离
```

设计文档必须说明：

- 执行入口。
- 生命周期。
- 数据缓存策略。
- 热更新或重载策略。
- 调试信息如何注入。
- 运行时是否依赖 UnityEngine。
- 是否需要对象池或 Reset。

参考模块：`FlowGraph.Runtime`、`LiteECS`、`ViewECS`、`EasyConfig.Loader`。

### 模板 C：Attribute + 代码生成模块

适用于需要业务声明类型、框架生成样板代码的系统。

推荐结构：

```text
Runtime/
├── Attribute/
├── Interface/
├── RuntimeData/
└── Execution/

Editor/
├── TypeCollector/
├── CodeGen/
└── Menu/
```

设计文档必须说明：

- 用户需要写哪些类型和 Attribute。
- 生成器扫描哪些程序集或类型。
- 生成哪些文件。
- 哪些文件可覆盖，哪些文件不可覆盖。
- 运行时如何调用生成结果。
- 新增类型后如何重新生成。

参考模块：`FlowGraph.CodeGen`、`ECS.Editor.Generator`、`Common.DataVisit`、`Common.StructSequence`。

### 模板 D：共享基础设施

适用于准备放入 `Common` 的能力。

设计前必须证明：

- 至少两个模块会复用。
- 它不包含单一业务模块的领域概念。
- API 足够稳定，不会迫使所有模块跟着频繁变动。
- Editor 与 Runtime 边界清楚。

设计文档必须说明：

- 目标复用场景。
- 不放在业务模块内的理由。
- 与现有 Common 能力的关系。
- 扩展方式。
- 向后兼容策略。

参考模块：`PropertyEditor`、`Timeline`、`TypeCollector`、`CSharpCodeWriter`、`ScriptObjectCollector`。

---

## 架构文档推荐结构

Agent 为本项目输出架构设计文档时，优先使用以下结构：

```markdown
# 标题

## 背景与目标
说明要解决的用户工作流、模块痛点和非目标。

## 当前架构理解
说明相关模块现状、已有扩展点、数据流和限制。

## 设计原则
明确本方案如何遵循 Editor/Runtime 分离、Attribute 驱动、代码生成、Context 生命周期和 Common 复用。

## 模块边界
用表格拆分 Runtime、Editor、Common、生成代码和业务侧需要实现的内容。

## 核心数据模型
列出编辑模型、运行模型、导出模型和必要 ID / Key / Context。

## 扩展点
列出 Attribute、接口、Provider、基类、事件和生成器入口。

## 数据流与生命周期
说明从创建、编辑、导出、加载、执行、热更新到销毁的流程。

## Editor 设计
说明窗口、Inspector、VisualElement、事件、Undo、预览和校验。

## Runtime 设计
说明 API、Context、缓存、执行、性能、调试和错误处理。

## 代码生成
如果有生成器，说明扫描规则、输出文件、覆盖策略和重新生成时机。

## 兼容性与迁移
说明现有数据、资产、API 和生成文件如何兼容。

## 风险与取舍
说明为什么选择该方案，以及放弃了哪些替代方案。

## 验证方式
说明 Editor 验证、Runtime 验证、生成器验证和手动 Unity 验证入口。

## 待确认问题
只保留真正阻塞实现的开放问题。
```

---

## 设计文档中的必备表格

### 模块边界表

| 区域 | 职责 | 主要类型 | 是否进入 Runtime | 说明 |
| --- | --- | --- | --- | --- |
| Runtime |  |  | 是/否 |  |
| Editor |  |  | 否 |  |
| Common |  |  | 视情况 |  |
| Generated |  |  | 视情况 |  |
| User Code |  |  | 视情况 |  |

### 扩展点表

| 扩展点 | 类型 | 用户如何接入 | 框架如何发现 | 运行时是否使用 |
| --- | --- | --- | --- | --- |

### 数据流表

| 阶段 | 输入 | 处理者 | 输出 | 备注 |
| --- | --- | --- | --- | --- |
| 编辑 |  |  |  |  |
| 导出 |  |  |  |  |
| 加载 |  |  |  |  |
| 执行 |  |  |  |  |
| 清理 |  |  |  |  |

### 生命周期表

| 生命周期 | 触发时机 | 负责对象 | 主要动作 |
| --- | --- | --- | --- |

### 风险表

| 风险 | 影响 | 缓解方式 |
| --- | --- | --- |

---

## Agent 输出前检查清单

输出架构设计文档前，Agent 应逐项检查：

- 是否明确区分了 Editor、Runtime、Common 和 Generated。
- 是否避免在 Runtime 热路径中使用反射扫描。
- 是否优先复用了 `Common` 现有能力。
- 是否说明了 Attribute、接口、Provider 或基类扩展方式。
- 是否说明了编辑资产与运行时数据的转换关系。
- 是否说明了 Context 的职责和生命周期。
- 如果涉及 UIToolkit，是否使用自定义事件冒泡。
- 如果涉及代码生成，是否区分了自动生成文件和手写文件。
- 是否说明了 Undo、Dirty、Asset 保存或缓存失效策略。
- 是否说明了验证方式，尤其是 Unity Editor 中如何验证。
- 是否没有新增未被请求的功能或跨模块重构。
- 是否同步更新对应 `Doc/` 文档和索引。

---

## 常见错误与修正方向

| 错误设计 | 为什么不合适 | 修正方向 |
| --- | --- | --- |
| 在 Runtime 中扫描所有程序集寻找扩展类型 | 与项目运行时轻量化方向冲突 | Editor 扫描并生成注册代码 |
| 给每个 UI 控件传入业务回调 | UI 与上下文耦合，难以复用 | 定义专用 `EventBase<T>` 并冒泡 |
| 新模块自己写一套字段编辑器 | 与 `PropertyEditor` 重复 | 扩展 `PropertyElement` 或字段 Attribute |
| 编辑资产直接保存运行时临时状态 | Undo、预览、导出和构建边界混乱 | 使用 RuntimeData 或 Context 存放运行态 |
| 生成器覆盖用户手写逻辑 | 容易丢代码 | 使用 `partial`，自动文件与手写文件分离 |
| 把单一模块能力放进 Common | Common 膨胀，形成错误依赖 | 先放模块内部，等复用需求明确后再沉淀 |
| 设计文档只有类图，没有生命周期 | 实现者仍需重新推导执行流程 | 增加数据流和生命周期章节 |

---

## 可复用的项目设计语句

Agent 可以在设计文档中复用以下表达，以保持项目术语一致：

- 本方案沿用框架的 Editor/Runtime 分离方式：Editor 负责编辑、扫描、校验和导出，Runtime 只保留执行所需的数据结构和调度入口。
- 业务侧通过 Attribute 和接口声明扩展意图，框架在 Editor 侧发现并生成或缓存注册信息。
- 运行时不依赖 AssetDatabase 或程序集扫描，所有高频路径通过生成代码、静态泛型入口或 Collector 访问。
- 编辑资产只保存可编辑数据和编辑器状态，运行前导出为 RuntimeData。
- UI 子元素通过自定义 `EventBase<T>` 事件向父级冒泡，业务决策集中在 Context 或上层 View。
- 新增生成器必须区分自动生成文件和手写实现文件，避免覆盖业务逻辑。
- Common 只承载跨模块基础能力，模块专属概念保留在模块内部。

---

## 阅读范围

本文设计思路主要来自以下模块：

- `Packages/com.kyle.action-framework/EasyConfig/`
- `Packages/com.kyle.action-framework/ActionLine/`
- `Packages/com.kyle.action-framework/ECS/`
- `Packages/com.kyle.action-framework/FlowGraph/`
- `Packages/com.kyle.action-framework/Common/`
- `Packages/com.kyle.action-framework/Doc/`
