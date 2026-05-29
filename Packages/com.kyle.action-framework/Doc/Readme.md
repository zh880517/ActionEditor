# Action Framework 文档索引

**com.kyle.action-framework** 是一个 Unity Editor 插件框架，提供可视化编辑器工具与运行时执行系统。适用于 Unity 6000.3.10f1，使用 C# 9.0 / .NET Standard 2.1。

---

## 模块文档

### 编辑器子系统

| 模块 | 简介 | 文档 |
|------|------|------|
| **ActionLine** | 基于帧的时间轴/Clip 编辑器框架，支持多选、复制粘贴、撤销重做、资源变体继承和实时预览 | [ActionLine 用户手册](ActionLine.md) |
| **FlowGraph** | 可视化脚本系统，支持数据流与控制流分离、嵌套子图、代码生成和运行时调试追踪 | [FlowGraph 用户手册](FlowGraph.md) |
| **GOAP** | 面向目标的行动规划系统（Goal-Oriented Action Planning），用于 NPC AI 自动规划行动序列 | [GOAP 用户手册](GOAP.md) |
| **LiteAnim** | 基于 PlayableGraph 的轻量动画图，支持多层叠加、Clip 拼接、混合树与状态过渡 | [LiteAnim 用户手册](LiteAnim.md) |
| **ECS** | 轻量实体组件系统，包含纯逻辑 `LiteECS` 与绑定 `GameObject` 的 `ViewECS`，支持组件扫描、代码生成、Group 遍历和 Reactive 更新 | [ECS 用户手册](ECS.md) |

### 配置与资源管理

| 模块 | 简介 | 文档 |
|------|------|------|
| **EasyConfig** | 游戏配置管理框架，支持 Excel 表格配置和 ScriptableObject 实体配置两种模式 | [EasyConfig 操作手册](EasyConfig.md) |
| **NamedAsset** | 基于名称的资源管理框架，统一 Editor（AssetDatabase）和 Runtime（AssetBundle）加载模式，含 GameObject 对象池 | [NamedAsset 操作手册](NamedAsset.md) |

### 调试与可视化

| 模块 | 简介 | 文档 |
|------|------|------|
| **VisualShape** | Unity 调试绘制库，支持编辑器与运行时绘制线条、形状、文本等调试内容，兼容内置管线与 URP | [VisualShape 使用手册](VisualShape.md) |

### Common 基础库

| 模块 | 简介 | 文档 |
|------|------|------|
| **Common（总览）** | PropertyEditor、Timeline 控件、类型收集器、对象池、代码生成等共享基础设施 | [Common 基础库总览](Common/Readme.md) |
| **PropertyEditor** | 反射驱动的通用属性 UI，25+ 内置类型编辑器，支持类型级和字段级自定义扩展 | [PropertyEditor 操作手册](Common/PropertyEditor.md) |
| **DataVisit** | 基于 Attribute 标记和代码生成的高性能二进制序列化框架（无反射），支持 SevenBit 和 RawBit 两套协议 | [DataVisit 操作手册](Common/DataVisit.md) |
| **StructSequence** | 单线程非托管内存结构体队列，用于生产者/消费者场景下的高性能消息传递 | [StructSequence 操作手册](Common/StructSequence.md) |

---

## 框架架构概览

```
com.kyle.action-framework/
├── ActionLine/      # 时间轴编辑器（Editor-only）
├── FlowGraph/       # 可视化脚本（Editor + Runtime）
├── LiteAnim/        # 轻量级动画图，基于 PlayableGraph（Editor + Runtime）
├── GOAP/            # 目标规划 AI（Editor + Runtime）
├── ECS/             # 轻量实体组件系统（Editor + Runtime）
├── EasyConfig/      # 配置管理（Editor + Runtime）
├── NamedAsset/      # 资源管理（Editor + Runtime）
├── VisualShape/     # 调试绘制（Editor + Runtime）
├── Common/          # 公共基础库（PropertyEditor、DataVisit、Serialization 等）
└── Doc/             # 文档目录（当前位置）
```

### 关键设计模式

- **Attribute 驱动扩展**：通过 C# Attribute 注册节点、属性编辑器和行为，无需手动修改注册代码
- **Assembly Definition 隔离**：多个程序集严格划分模块边界，Editor 程序集仅在编辑器中编译
- **UIToolkit 事件通信**：子 VisualElement 向父级通信使用自定义 `EventBase<T>` 事件冒泡，不使用构造函数传递 delegate

---

## LiteAnim

LiteAnim 是基于 Unity PlayableGraph API 的轻量级动画图，支持动画分层、混合和状态过渡，`LiteAnimAsset` 为数据容器，`LiteAnimGraph` 封装 PlayableGraph。当前暂无独立文档。
