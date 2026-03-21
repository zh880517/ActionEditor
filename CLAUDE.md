# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ActionEditor is a **Unity Editor plugin framework** (Unity 6000.3.10f1) for creating visual editor tools and runtime execution systems. It uses C# 9.0 with .NET Standard 2.1.

## Development Environment

- **IDE**: Visual Studio 2022 or VS Code (with `.vscode/` config)
- **Unity Version**: 6000.3.10f1 (Unity 6)
- **Build**: Managed entirely by Unity Editor — open the project in Unity to compile
- **Tests**: Run via Unity Editor menus; NUnit via `com.unity.test-framework`; test entry points are in `Assets/Script/FlowGraphTest/` and `Assets/TestData/`
- **Editor Test Window**: `Tools/VisualElementTest` menu item

There are no standalone CLI build or test commands — everything runs through the Unity Editor.

## Architecture

The framework lives in `Packages/com.kyle.action-framework/` and consists of four independent subsystems:

### ActionLine (Editor-only)
Timeline/clip-based action sequencing editor built with IMGUI + UIToolkit. Manages clips on tracks with frame-based timelines. Key sub-folders: `ElementView/` (UI), `EditorAction/` (operations like create/delete/copy/paste), `Manipulator/` (input), `Preview/` (playback).

### FlowGraph (Editor + Runtime)
Node-based visual scripting system. The editor handles visual layout; the runtime (`FlowGraph/Runtime/`) handles execution. Nodes are defined with attributes (`[FlowNodePath]`, `[FlowNodeTag]`). Supports code generation and debug tracing.

### LiteAnim (Editor + Runtime)
Lightweight animation graph built on Unity's PlayableGraph API. Supports animation layering, blending, and state transitions. `LiteAnimAsset` is the data container; `LiteAnimGraph` wraps PlayableGraph.

### Common (Shared Foundation)
Used by all other subsystems:
- **PropertyEditor** (`Common/Editor/PropertyEditor/`): Reflection-based generic property UI. `PropertyElementFactory.cs` creates UIToolkit elements for any serializable type. Add new type editors in `BuiltIn/`.
- **DataVisit** (`Common/Editor/DataVisit/`): Type visitor pattern for serialization.
- **Serialization** (`Common/Runtime/Serialization/`): Binary packing with 7-bit and raw-bit encoding. Unsafe code is enabled here.
- **Attributes** (`Common/Runtime/Attribute/`): `[DisplayName]`, `[ReadOnly]`, `[ArraySize]`, etc. Drive both UI generation and runtime behavior.

## Key Patterns

- **Attribute-driven extensibility**: Register new nodes, property editors, and behaviors via C# attributes rather than code modification.
- **Assembly Definition files** (`.asmdef`): 7 assemblies enforce module boundaries. Editor assemblies only compile in Unity Editor; Runtime assemblies compile for all platforms.
- **Type discovery**: `TypeCollection` uses reflection at startup to find all types decorated with framework attributes.
- **Object pooling**: `Recycle/` namespace provides pooling utilities used throughout runtime code.
