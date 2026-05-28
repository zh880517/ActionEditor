# AGENTS.md

This file provides guidance to Codex when working with code in this repository.

## 项目概览

ActionEditor 是一个 Unity Editor 插件框架，用于创建可视化编辑器工具和运行时执行系统。

- Unity 版本：`6000.3.10f1`
- 语言和运行时：C# 9.0 / .NET Standard 2.1
- 包路径：`Packages/com.kyle.action-framework/`
- 构建方式：由 Unity Editor 管理，没有独立的 CLI 构建命令
- 测试方式：通过 Unity Editor 菜单运行，包括 `Tools/VisualElementTest`；NUnit 由 `com.unity.test-framework` 提供

## 文档

所有模块文档都位于 `Packages/com.kyle.action-framework/Doc/`。

| 文档 | 说明 |
| --- | --- |
| `Doc/Readme.md` | 框架总索引，所有模块入口 |
| `Doc/ActionLine.md` | 时间轴和 Clip 编辑器 |
| `Doc/FlowGraph.md` | 可视化脚本系统 |
| `Doc/GOAP.md` | 目标规划 AI |
| `Doc/LiteAnim.md` | 基于 PlayableGraph 的轻量动画图 |
| `Doc/EasyConfig.md` | 配置管理框架 |
| `Doc/NamedAsset.md` | 资源管理框架 |
| `Doc/VisualShape.md` | 调试绘制库 |
| `Doc/Common/Readme.md` | Common 基础库索引 |

## 架构

框架由以下子系统组成，均位于 `Packages/com.kyle.action-framework/`。

| 模块 | 编译范围 | 简介 |
| --- | --- | --- |
| `ActionLine/` | Editor-only | 帧级时间轴和 Clip 编辑器，使用 IMGUI 与 UIToolkit |
| `FlowGraph/` | Editor + Runtime | 可视化节点脚本，支持代码生成和调试追踪 |
| `LiteAnim/` | Editor + Runtime | 基于 PlayableGraph 的轻量动画图 |
| `GOAP/` | Editor + Runtime | 面向目标的行动规划 AI |
| `EasyConfig/` | Editor + Runtime | Excel 与 ScriptableObject 双模式配置管理 |
| `NamedAsset/` | Editor + Runtime | 基于名称的 AssetBundle 资源管理 |
| `VisualShape/` | Editor + Runtime | 调试绘制库，兼容内置管线与 URP |
| `Common/` | Editor + Runtime | 共享基础库 |

### Common 基础库

| 子模块 | 简介 |
| --- | --- |
| `PropertyEditor` | 反射驱动的通用属性 UI，基于 UIToolkit，通过 `[CustomPropertyElement]` 扩展 |
| `Timeline` | 可复用 UIToolkit 时间轴控件，包括 `TimelineView`、`TrackView` 和 `ClipView` |
| `DataVisit` | Attribute 驱动和代码生成的低反射二进制序列化，包括 SevenBit 与 RawBit |
| `StructSequence` | 非托管内存结构体消息队列 |
| `Serialization` | JSON 与 GUID 多态序列化，包括 `TypeSerializerHelper` |
| `TypeCollector` | 反射类型收集器和框架扩展点发现机制 |
| `Recycle` | 轻量泛型对象池 |

## 关键模式

- Attribute 驱动扩展：通过 C# Attribute 注册节点、属性编辑器和行为，不写手动注册代码。
- 程序集边界是刻意设计的一部分。Editor 程序集只在 Unity Editor 中编译，Runtime 程序集面向所有平台。
- 类型发现使用 `TypeCollector`、`AttributeTagTypeCollector` 和 `TypeWithAttributeCollector` 通过反射扫描程序集。
- UIToolkit 子级到父级的通信应使用自定义 `EventBase<T>` 事件，并通过 `SendEvent` 和冒泡传递。不要为这种交互在构造函数中传递 `Action` 回调或委托。每种交互定义专用事件类。
- 代码注释和仓库文档使用中文。

## 规则

- 禁止创建 `.asmdef` 文件，除非用户明确要求。现有程序集边界属于模块设计的一部分。
- 修改或新增模块功能后，必须同步更新 `Packages/com.kyle.action-framework/Doc/` 下对应文档。
- 如果新增模块，还要更新 `Doc/Readme.md` 和相关子目录的 `Readme.md`。
- 所有注释和文档字符串使用中文。
- 不要添加未被请求的功能、重构或“改进”。
- 优先沿用项目现有模式和本地辅助 API，不要轻易引入新抽象。

<!-- CODEGRAPH_START -->
## CodeGraph

本项目配置了 CodeGraph MCP server，可以通过 `codegraph_*` 工具读取基于 tree-sitter 解析得到的符号、调用边和文件结构。

### 何时优先使用 CodeGraph

结构性问题优先使用 CodeGraph，例如：谁调用了谁、修改某个符号会影响哪里、某个符号在哪里定义、签名是什么。只有查询字面文本时才优先使用原生搜索，例如字符串内容、注释、日志文本，或已经打开了明确文件之后的局部查找。

| 问题 | 工具 |
| --- | --- |
| X 在哪里定义？查找名为 X 的符号 | `codegraph_search` |
| 谁调用了函数 Y？ | `codegraph_callers` |
| 函数 Y 调用了什么？ | `codegraph_callees` |
| X 如何到达或变成 Y？ | `codegraph_trace` |
| 修改 Z 会影响哪里？ | `codegraph_impact` |
| 查看 Y 的签名、源码或文档注释 | `codegraph_node` |
| 为某个任务或区域获取上下文 | `codegraph_context` |
| 一次查看多个相关符号源码 | `codegraph_explore` |
| 查看某个路径下有哪些文件 | `codegraph_files` |
| 检查索引是否健康 | `codegraph_status` |

### CodeGraph 规则

- 回答架构问题或“X 如何工作”时，先用 `codegraph_context`，需要源码时再用一次 `codegraph_explore`。
- 分析从一个符号到另一个符号的具体流程时，先用 `codegraph_trace`。
- 对已索引文件，信任 CodeGraph 的结构结果，不要再用 grep 重查符号结构。
- 需要上下文时，不要用 `codegraph_search` 串联多个 `codegraph_node`；优先用 `codegraph_context` 或 `codegraph_explore` 一次拿到。
- 如果 CodeGraph 提示某些文件等待同步，直接读取这些文件获取最新内容；没有被列为等待同步的文件可以视为新鲜。
- 如果 `.codegraph/` 不存在且 CodeGraph 报告项目未初始化，询问用户是否运行 `codegraph init -i`。
<!-- CODEGRAPH_END -->
