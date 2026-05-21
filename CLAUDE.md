# CLAUDE.md

This file provides guidance to AI assistants when working with code in this repository.

## Project Overview

ActionEditor is a **Unity Editor plugin framework** (Unity 6000.3.10f1) for creating visual editor tools and runtime execution systems. Uses C# 9.0 / .NET Standard 2.1.

- **Package path**: `Packages/com.kyle.action-framework/`
- **Build**: Managed entirely by Unity Editor — no standalone CLI build commands
- **Tests**: Run via Unity Editor menus (`Tools/VisualElementTest`); NUnit via `com.unity.test-framework`

## Documentation

All module documentation lives in `Packages/com.kyle.action-framework/Doc/`.

| 文档 | 说明 |
|------|------|
| [Doc/Readme.md](Packages/com.kyle.action-framework/Doc/Readme.md) | 框架总索引，所有模块入口 |
| [Doc/ActionLine.md](Packages/com.kyle.action-framework/Doc/ActionLine.md) | 时间轴/Clip 编辑器 |
| [Doc/FlowGraph.md](Packages/com.kyle.action-framework/Doc/FlowGraph.md) | 可视化脚本系统 |
| [Doc/GOAP.md](Packages/com.kyle.action-framework/Doc/GOAP.md) | 目标规划 AI |
| [Doc/LiteAnim.md](Packages/com.kyle.action-framework/Doc/LiteAnim.md) | 轻量动画图（PlayableGraph） |
| [Doc/EasyConfig.md](Packages/com.kyle.action-framework/Doc/EasyConfig.md) | 配置管理框架 |
| [Doc/NamedAsset.md](Packages/com.kyle.action-framework/Doc/NamedAsset.md) | 资源管理框架 |
| [Doc/VisualShape.md](Packages/com.kyle.action-framework/Doc/VisualShape.md) | 调试绘制库 |
| [Doc/Common/Readme.md](Packages/com.kyle.action-framework/Doc/Common/Readme.md) | Common 基础库索引 |

## Architecture

The framework consists of these subsystems (all under `Packages/com.kyle.action-framework/`):

| 模块 | 编译范围 | 简介 |
|------|---------|------|
| `ActionLine/` | Editor-only | 帧级时间轴/Clip 编辑器，IMGUI + UIToolkit |
| `FlowGraph/` | Editor + Runtime | 可视化节点脚本，支持代码生成和调试追踪 |
| `LiteAnim/` | Editor + Runtime | 基于 PlayableGraph 的轻量动画图 |
| `GOAP/` | Editor + Runtime | 面向目标行动规划 AI |
| `EasyConfig/` | Editor + Runtime | Excel + ScriptableObject 双模式配置管理 |
| `NamedAsset/` | Editor + Runtime | 基于名称的 AssetBundle 资源管理 |
| `VisualShape/` | Editor + Runtime | 调试绘制库，兼容内置管线与 URP |
| `Common/` | Editor + Runtime | 共享基础库（见下） |

### Common 基础库

| 子模块 | 简介 |
|--------|------|
| `PropertyEditor` | 反射驱动的通用属性 UI，UIToolkit，`[CustomPropertyElement]` 扩展 |
| `Timeline` | 可复用 UIToolkit 时间轴控件（`TimelineView` / `TrackView` / `ClipView`） |
| `DataVisit` | Attribute 驱动 + 代码生成的零反射二进制序列化（SevenBit / RawBit） |
| `StructSequence` | 非托管内存结构体消息队列 |
| `Serialization` | JSON + GUID 多态序列化（`TypeSerializerHelper`） |
| `TypeCollector` | 反射类型收集器三件套，框架扩展点发现机制 |
| `Recycle` | 轻量泛型对象池 |

## Key Patterns

- **Attribute-driven extensibility**: Register nodes, property editors, and behaviors via C# attributes — no manual registration code.
- **Assembly Definition boundaries**: 7 `.asmdef` files enforce module isolation. Editor assemblies compile only in Unity Editor; Runtime assemblies compile for all platforms.
- **Type discovery**: `TypeCollector` / `AttributeTagTypeCollector` / `TypeWithAttributeCollector` scan assemblies at startup via reflection.
- **UIToolkit event propagation**: Child-to-parent communication uses custom `EventBase<T>` events (`SendEvent` + bubbling). Do **not** pass `Action` callbacks or delegates through constructors for this purpose. Define a dedicated event class per interaction.
- **Code comments**: Use Chinese for all comments and documentation within this repository.

## Rules

- **禁止创建 `.asmdef` 文件**，除非用户明确要求。现有 7 个程序集的边界已确定，擅自新增会破坏模块隔离。
- **功能模块变更时同步更新文档**：修改或新增某模块的功能后，必须同步更新 `Doc/` 下对应的文档文件；若涉及新模块，还需在 `Doc/Readme.md` 和所属子目录的 `Readme.md` 中补充索引条目。
- 所有注释、文档字符串使用中文。
- 不要添加未被请求的功能、重构或"改进"。

