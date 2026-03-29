# ActionLine 用户手册

## 目录

- [概述](#概述)
- [核心概念](#核心概念)
- [快速开始](#快速开始)
- [编辑器操作指南](#编辑器操作指南)
- [扩展开发指南](#扩展开发指南)
  - [自定义 Clip 类型](#自定义-clip-类型)
  - [自定义 Clip 编辑器](#自定义-clip-编辑器)
  - [自定义预览系统](#自定义预览系统)
  - [自定义右键菜单操作](#自定义右键菜单操作)
  - [自定义 EditorProvider](#自定义-editorprovider)
- [资源变体系统](#资源变体系统)
- [属性参考](#属性参考)
- [快捷键参考](#快捷键参考)
- [API 参考](#api-参考)

---

## 概述

ActionLine 是一个基于 Unity Editor 的时间轴/Clip 编辑器框架。它提供了一套可视化的时间轴编辑界面，支持在帧级别精度上管理和编辑动作片段（Clip）。

**核心特性：**

- 基于帧的时间轴编辑
- 可视化的 Clip 创建、移动、缩放
- 支持多选、复制/粘贴、撤销/重做
- 资源变体（Variant）继承系统
- 编辑器内实时预览
- 完全通过属性（Attribute）驱动的扩展机制 —— 无需手动注册

---

## 核心概念

### ActionLineAsset

`ActionLineAsset` 是时间轴数据的容器，继承自 `ScriptableObject`。每个 Asset 包含：

- **帧数（FrameCount）**：时间轴的总帧数
- **Clip 列表**：该 Asset 上的所有 Clip
- **变体源（Source）**：可选的父级 Asset，用于继承

### ActionLineClip

`ActionLineClip` 是时间轴上的一个动作片段，继承自 `ScriptableObject`。每个 Clip 包含：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Owner` | `ActionLineAsset` | 所属的 Asset（只读） |
| `Disable` | `bool` | 是否禁用 |
| `Description` | `string` | 描述信息 |
| `StartFrame` | `int` | 起始帧 |
| `Length` | `int` | 帧长度（默认为 1） |

### Track（轨道）

每个 Clip 对应一条轨道。轨道左侧显示标题（名称、图标、启用按钮），右侧显示 Clip 在时间轴上的位置和长度。

---

## 快速开始

### 第一步：创建自定义 Asset 类型

```csharp
using ActionLine;
using UnityEngine;

// 创建自己的时间轴资源类型
public class CombatActionAsset : ActionLineAsset
{
    // 可添加额外的自定义数据
    public float PlaySpeed = 1.0f;
}
```

### 第二步：创建自定义 Clip 类型

```csharp
using ActionLine;
using UnityEngine;

// 通过 ActionLineType 关联到你的 Asset 类型
[ActionLineType(typeof(CombatActionAsset))]
// 通过 ActionClipColor 设置 Clip 在时间轴上的颜色
[ActionClipColor(0.2f, 0.6f, 1.0f)]
// 通过 Alias 设置显示名称（用于创建菜单）
[Alias("移动")]
public class MoveClip : ActionLineClip
{
    public Vector3 TargetPosition;
    public float Speed = 5f;
}

[ActionLineType(typeof(CombatActionAsset))]
[ActionClipColor(1.0f, 0.3f, 0.3f)]
[Alias("攻击")]
public class AttackClip : ActionLineClip
{
    public int Damage = 10;
    public float AttackRange = 2f;
}
```

### 第三步：在编辑器中使用

1. 在 Unity 中创建一个 `CombatActionAsset`（通过 `Assets > Create` 或代码创建）
2. 使用 ActionLine 编辑器打开该 Asset
3. 右键或按 `Ctrl+N` 创建 Clip
4. 在弹出的类型选择窗口中选择 `移动` 或 `攻击`
5. 拖拽 Clip 调整位置和时长

---

## 编辑器操作指南

### 界面布局

```
┌────────────┬─────────────────────────────┬──────────────┐
│  轨道标题   │         时间轴区域           │   属性面板    │
│            │                             │              │
│  [名称]    │  ┌──────┐     ┌─────────┐   │  选中 Clip   │
│  [名称]    │  │ Clip │     │  Clip   │   │  的属性检视   │
│  [名称]    │  └──────┘     └─────────┘   │              │
│            │                             │              │
│            │  ▼ 帧指针                    │              │
└────────────┴─────────────────────────────┴──────────────┘
                 刻度栏（帧号）
```

- **轨道标题区（左侧）**：显示每条轨道的名称、图标、启用/禁用按钮
- **时间轴区域（中间）**：显示 Clip 在时间线上的位置，支持拖拽和缩放
- **属性面板（右侧）**：显示选中 Clip 的属性，支持编辑
- **刻度栏（顶部）**：显示帧号，点击可移动帧指针

### Clip 操作

| 操作 | 方式 |
|------|------|
| 创建 Clip | 右键菜单 > Create，或按 `Ctrl+N` |
| 选择 Clip | 左键点击 Clip |
| 多选 Clip | 按住 `Ctrl` 点击多个 Clip |
| 移动 Clip | 左键拖拽 Clip 中间区域 |
| 调整起始帧 | 左键拖拽 Clip 左边缘 |
| 调整结束帧 | 左键拖拽 Clip 右边缘 |
| 删除 Clip | 选中后按 `Delete`，或右键菜单 > Delete |
| 复制 Clip | `Ctrl+C` |
| 粘贴 Clip | `Ctrl+V` |
| 复制并粘贴 | `Ctrl+D` |
| 全选 | `Ctrl+A` |

### 轨道操作

| 操作 | 方式 |
|------|------|
| 选择轨道 | 左键点击轨道标题 |
| 多选轨道 | 按住 `Ctrl` 点击多个轨道标题 |
| 拖拽排序 | 左键拖拽轨道标题上下移动 |
| 启用/禁用 | 点击轨道标题的可见性按钮 |
| 右键菜单 | 右键点击轨道标题 |

### 视口操作

| 操作 | 方式 |
|------|------|
| 移动帧指针 | 点击刻度栏 |
| 水平滚动 | 水平滚动条，或鼠标滚轮 |
| 垂直滚动 | 垂直滚动条 |
| 缩放 | 缩放滑块 |

### 预览

启用预览后，移动帧指针会实时触发 Clip 的预览模拟。预览系统会：

- 当帧指针进入 Clip 范围时调用 `Start`
- 帧指针在 Clip 范围内移动时调用 `Update`
- 当帧指针离开 Clip 范围时调用 `End`
- 每帧调用 `DrawGizmos` 绘制辅助线

---

## 扩展开发指南

ActionLine 采用**属性驱动的扩展机制**，所有扩展类通过属性标记后自动被框架发现，无需手动注册。

### 自定义 Clip 类型

#### 基本步骤

1. 创建继承自 `ActionLineClip` 的类
2. 添加 `[ActionLineType]` 属性关联到目标 Asset 类型
3. （可选）添加 `[ActionClipColor]` 设置颜色
4. （可选）添加 `[Alias]` 设置显示名称

#### 示例

```csharp
using ActionLine;
using UnityEngine;

[ActionLineType(typeof(CombatActionAsset))]
[ActionClipColor(0.8f, 0.4f, 0.1f)]
[Alias("播放特效")]
public class PlayEffectClip : ActionLineClip
{
    [Combined, Display("特效预制体")]
    public GameObject EffectPrefab;

    [Combined, Display("偏移位置")]
    public Vector3 Offset;

    [Combined, Display("缩放")]
    public float Scale = 1.0f;

    [Combined, Display("跟随角色")]
    public bool FollowOwner = true;
}
```

#### 属性装饰器

这些属性可用于控制 Clip 字段在属性面板中的显示方式：

| 属性 | 说明 |
|------|------|
| `[Combined]` | 将字段分组显示 |
| `[Display("标签", "提示")]` | 设置显示名称和工具提示 |
| `[ReadOnly]` | 只读，不可编辑 |
| `[Multiline]` | 多行文本输入 |
| `[PropertyMotion]` | 支持动画关键帧风格的控制 |

#### 一个 Clip 可以关联多个 Asset 类型

```csharp
[ActionLineType(typeof(CombatActionAsset))]
[ActionLineType(typeof(CutsceneActionAsset))]
public class PlaySoundClip : ActionLineClip
{
    public AudioClip AudioClip;
    public float Volume = 1.0f;
}
```

---

### 自定义 Clip 编辑器

通过 `ActionClipEditor` 可以自定义 Clip 在编辑器中的外观和交互行为。

#### 基本步骤

1. 创建继承自 `ActionClipEditor` 的类
2. 添加 `[CustomClipEditor(typeof(YourClipType))]` 属性

#### 可覆盖的方法

```csharp
public class ActionClipEditor
{
    // 当前关联的 Clip
    public ActionLineClip Clip { get; }

    // 创建轨道标题区的自定义 UI 元素（返回 null 使用默认显示）
    public virtual VisualElement CreateCutomTitleElement(ActionLineClip clip);

    // 创建时间轴上 Clip 内部的自定义 UI 元素（返回 null 使用默认显示）
    public virtual VisualElement CreateCustomContentElement(ActionLineClip clip);

    // 右键点击 Clip 时的菜单扩展
    // frameOffset：鼠标在 Clip 内的帧偏移
    public virtual void OnClipMenu(ActionLineClip clip, int frameOffset, GenericMenu menu);

    // 右键点击轨道标题时的菜单扩展
    public virtual void OnTitleMenu(ActionLineClip clip, GenericMenu menu);

    // 按下 Alt + 按键时的响应（仅当帧指针在 Clip 范围内时触发）
    // 返回 true 表示已处理该按键
    public virtual bool OnKeyDown(ActionLineClip clip, bool shiftDown, KeyCode keyCode, int frameOffset);

    // 自定义 Clip 在时间轴上的显示名称
    public virtual string GetClipShowName(ActionLineClip clip);
}
```

#### 完整示例

```csharp
using ActionLine;
using ActionLine.EditorView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomClipEditor(typeof(PlayEffectClip))]
public class PlayEffectClipEditor : ActionClipEditor
{
    // 自定义时间轴上 Clip 的显示内容
    public override VisualElement CreateCustomContentElement(ActionLineClip clip)
    {
        var effectClip = clip as PlayEffectClip;
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;

        var icon = new Image();
        icon.image = EditorGUIUtility.IconContent("ParticleSystem Icon").image;
        icon.style.width = 16;
        icon.style.height = 16;
        container.Add(icon);

        var label = new Label(effectClip.EffectPrefab ? effectClip.EffectPrefab.name : "(无特效)");
        container.Add(label);

        return container;
    }

    // 自定义显示名称
    public override string GetClipShowName(ActionLineClip clip)
    {
        var effectClip = clip as PlayEffectClip;
        if (effectClip.EffectPrefab != null)
            return $"特效: {effectClip.EffectPrefab.name}";
        return "特效: (未设置)";
    }

    // 扩展右键菜单
    public override void OnClipMenu(ActionLineClip clip, int frameOffset, GenericMenu menu)
    {
        var effectClip = clip as PlayEffectClip;
        menu.AddItem(new GUIContent("定位特效资源"), false, () =>
        {
            if (effectClip.EffectPrefab != null)
                EditorGUIUtility.PingObject(effectClip.EffectPrefab);
        });
    }

    // 处理 Alt+快捷键
    public override bool OnKeyDown(ActionLineClip clip, bool shiftDown, KeyCode keyCode, int frameOffset)
    {
        if (keyCode == KeyCode.P)
        {
            Debug.Log($"预览特效，帧偏移: {frameOffset}");
            return true; // 已处理
        }
        return false;
    }
}
```

---

### 自定义预览系统

预览系统用于在编辑器中实时模拟 Clip 的效果（如播放动画、生成特效等）。

#### 基本步骤

1. 创建继承自 `TClipSimulator<T>` 的类（`T` 为你的 Clip 类型）
2. 添加 `[CustomClipPreview(typeof(YourClipType))]` 属性
3. 实现生命周期方法

#### IClipPreview 接口

```csharp
public interface IClipPreview
{
    Type ClipType { get; }

    // 帧指针进入 Clip 范围时调用
    void Start(ActionLinePreviewContext context, ActionLineClip clip);

    // 帧指针在 Clip 范围内时每帧调用
    // frameOffset：相对于 Clip 起始帧的偏移（0 到 Length-1）
    void Update(ActionLinePreviewContext context, ActionLineClip clip, int frameOffset);

    // 帧指针离开 Clip 范围时调用
    void End(ActionLinePreviewContext context, ActionLineClip clip);

    // 每帧调用，用于绘制 Gizmos
    // frameOffset：-1 表示帧指针不在 Clip 范围内
    // readOnly：true 表示这是一个继承的 Clip
    void DrawGizmos(ActionLinePreviewContext context, ActionLineClip clip, int frameOffset, bool readOnly);

    // 预览系统销毁时调用，用于清理资源
    void Destroy(ActionLinePreviewContext context);
}
```

#### 使用泛型基类（推荐）

`TClipSimulator<T>` 提供了类型安全的泛型封装，避免手动类型转换：

```csharp
using ActionLine;
using ActionLine.EditorView;
using UnityEngine;

[CustomClipPreview(typeof(PlayEffectClip))]
public class PlayEffectSimulator : TClipSimulator<PlayEffectClip>
{
    private GameObject effectInstance;

    protected override void OnStart(ActionLinePreviewContext context, PlayEffectClip clip)
    {
        if (clip.EffectPrefab != null)
        {
            effectInstance = Object.Instantiate(clip.EffectPrefab);
            effectInstance.hideFlags = HideFlags.DontSave;
            effectInstance.transform.position = clip.Offset;
            effectInstance.transform.localScale = Vector3.one * clip.Scale;
        }
    }

    protected override void OnUpdate(ActionLinePreviewContext context, PlayEffectClip clip, int frameOffset)
    {
        if (effectInstance != null)
        {
            // 根据帧偏移更新特效状态
            float progress = (float)frameOffset / Mathf.Max(1, clip.Length - 1);
            // ... 更新逻辑
        }
    }

    protected override void OnEnd(ActionLinePreviewContext context, PlayEffectClip clip)
    {
        if (effectInstance != null)
        {
            Object.DestroyImmediate(effectInstance);
            effectInstance = null;
        }
    }

    protected override void OnDrawGizmos(ActionLinePreviewContext context, PlayEffectClip clip, int frameOffset, bool readOnly)
    {
        if (frameOffset >= 0)
        {
            Gizmos.color = readOnly ? Color.gray : Color.cyan;
            Gizmos.DrawWireSphere(clip.Offset, 0.5f);
        }
    }

    public override void Destroy(ActionLinePreviewContext context)
    {
        if (effectInstance != null)
        {
            Object.DestroyImmediate(effectInstance);
            effectInstance = null;
        }
    }
}
```

#### 预览生命周期图

```
帧指针移动：   ──────┬──────────────────────┬──────────
                     │    Clip 范围          │
                  Start                     End
                     │                      │
                     ├─ Update(0)           │
                     ├─ Update(1)           │
                     ├─ Update(2)           │
                     ├─ ...                 │
                     ├─ Update(Length-1)    │
                     │                      │

DrawGizmos：每帧都会调用，frameOffset=-1 表示不在 Clip 范围内
```

---

### 自定义右键菜单操作

通过 `EditorAction` 可以向右键菜单和快捷键系统添加自定义操作。

#### 基本步骤

1. 创建继承自 `EditorAction` 的类
2. （可选）添加 `[CustomEditorAction(typeof(YourAssetType))]` 限定到特定 Asset 类型
3. 实现菜单显示和执行逻辑

#### EditorAction 基类

```csharp
public abstract class EditorAction
{
    // 编辑器上下文，可访问选中的 Clip、目标 Asset 等
    public ActionLineEditorContext Context;

    // ── 菜单配置 ──

    // 菜单路径，如 "Tools/My Action"。返回 null 则不显示在菜单中
    public abstract string MenuPath { get; }

    // 排序优先级。每 100 为一组，组间会显示分割线
    // 例：0-99 第一组，100-199 第二组
    public virtual int ShowOrder => 0;

    // 菜单图标
    public virtual Texture Icon => null;

    // ── 快捷键配置 ──

    // 快捷键
    public virtual KeyCode ShortCutKey => KeyCode.None;

    // 是否需要同时按住 Ctrl/Cmd
    public virtual bool ActionKey => false;

    // 是否需要同时按住 Shift
    public virtual bool ShiftKey => false;

    // 是否在菜单上显示快捷键提示
    public virtual bool ShowShortCut => true;

    // ── 状态控制 ──

    // 在指定模式下是否可见
    public virtual bool Visable(ActionModeType mode);

    // 在指定模式下是否可用（Visable 返回 true 后才会检查）
    public virtual bool IsValid(ActionModeType mode);

    // 是否显示勾选标记
    public virtual bool IsOn(ActionModeType mode);

    // ── 执行 ──
    public virtual void Execute(ActionModeType mode);
}
```

#### ActionModeType 触发模式

| 模式 | 说明 |
|------|------|
| `Clip` | 右键点击 Clip |
| `ClipEmpty` | 右键点击时间轴空白区域 |
| `TrackTitle` | 右键点击轨道标题 |
| `TrackTitleEmpty` | 右键点击轨道标题空白区域 |
| `ShortCut` | 通过键盘快捷键触发 |

#### 完整示例

```csharp
using ActionLine;
using ActionLine.EditorView;
using UnityEngine;

// 不添加 CustomEditorAction 属性 = 对所有 Asset 类型生效
public class ResetClipAction : EditorAction
{
    public override string MenuPath => "Tools/Reset Clip";
    public override int ShowOrder => 200; // 第三组
    public override KeyCode ShortCutKey => KeyCode.R;
    public override bool ActionKey => true; // Ctrl+R

    public override bool Visable(ActionModeType mode)
    {
        // 仅在 Clip 相关模式和快捷键模式下显示
        return mode == ActionModeType.Clip || mode == ActionModeType.ShortCut;
    }

    public override bool IsValid(ActionModeType mode)
    {
        // 至少选中一个非继承的 Clip
        return Context.SelectedClips.Count > 0
            && !Context.SelectedClips.Exists(it => it.IsInherit);
    }

    public override void Execute(ActionModeType mode)
    {
        Context.RegisterUndo("Reset Clips");
        foreach (var data in Context.SelectedClips)
        {
            if (data.IsInherit)
                continue;
            data.Clip.StartFrame = 0;
            data.Clip.Length = 1;
        }
        Context.RefreshView();
    }
}
```

#### 仅对特定 Asset 类型生效

```csharp
[CustomEditorAction(typeof(CombatActionAsset))]
public class CombatSpecificAction : EditorAction
{
    public override string MenuPath => "Combat/Special Action";

    public override bool Visable(ActionModeType mode) => true;
    public override bool IsValid(ActionModeType mode) => true;

    public override void Execute(ActionModeType mode)
    {
        Debug.Log("仅在 CombatActionAsset 编辑器中可见");
    }
}
```

---

### 自定义 EditorProvider

`ActionLineEditorProvider` 是最高级别的扩展点，用于定制整个编辑器体验——包括编辑器上下文、预览系统和资源管理。

#### 基本步骤

1. 创建继承自 `ActionLineEditorProvider` 的类
2. 添加 `[CustomEditorProvider(typeof(YourAssetType))]` 属性
3. 覆盖需要定制的方法

#### 可覆盖的方法

```csharp
public class ActionLineEditorProvider
{
    // 创建编辑器上下文
    public virtual ActionLineEditorContext CreateEditorContext(ActionLineAsset asset);

    // 创建预览系统
    public virtual ActionLinePreviewContext CreatePreview(
        ActionLineAsset target,
        PreviewResourceContext resourceContext);

    // 创建预览资源管理器
    public virtual PreviewResourceContext CreateResourceContext();
}
```

#### 示例：自定义预览系统

```csharp
using ActionLine;
using ActionLine.EditorView;
using UnityEngine;

// 自定义资源管理器，持有预览所需的场景对象
public class CombatPreviewResourceContext : PreviewResourceContext
{
    public GameObject PreviewCharacter;
}

// 自定义预览上下文
public class CombatPreviewContext : ActionLinePreviewContext
{
    public override void OnCreate()
    {
        // 初始化预览场景
        var res = ResourceContext as CombatPreviewResourceContext;
        if (res != null && res.PreviewCharacter == null)
        {
            res.PreviewCharacter = new GameObject("PreviewCharacter");
            res.PreviewCharacter.hideFlags = HideFlags.DontSave;
        }
    }

    // 在每帧预览之前调用
    protected override void OnBeforFramePreview()
    {
        // 预处理逻辑
    }

    // 在每帧预览之后调用
    protected override void OnAfterFramePreview()
    {
        // 后处理逻辑
    }

    public override void Disable()
    {
        base.Disable();
        // 隐藏预览场景对象
        var res = ResourceContext as CombatPreviewResourceContext;
        if (res != null && res.PreviewCharacter != null)
            res.PreviewCharacter.SetActive(false);
    }

    public override void Destroy()
    {
        base.Destroy();
        var res = ResourceContext as CombatPreviewResourceContext;
        if (res != null && res.PreviewCharacter != null)
        {
            Object.DestroyImmediate(res.PreviewCharacter);
            res.PreviewCharacter = null;
        }
    }
}

// 注册 Provider
[CustomEditorProvider(typeof(CombatActionAsset))]
public class CombatEditorProvider : ActionLineEditorProvider
{
    public override ActionLinePreviewContext CreatePreview(
        ActionLineAsset target,
        PreviewResourceContext resourceContext)
    {
        var context = new CombatPreviewContext();
        context.Target = target;
        context.ResourceContext = resourceContext;
        return context;
    }

    public override PreviewResourceContext CreateResourceContext()
    {
        var context = ScriptableObject.CreateInstance<CombatPreviewResourceContext>();
        context.hideFlags = HideFlags.DontSave;
        return context;
    }
}
```

---

## 资源变体系统

ActionLine 支持资源变体（Variant），允许一个 Asset 继承另一个 Asset 的 Clip 数据。

### 概念

```
BaseActionAsset (Source)
├── Clip A
├── Clip B
└── Clip C

VariantActionAsset (Variant of Base)
├── [继承] Clip A  ← 只读，来自 Source
├── [继承] Clip B  ← 可以禁用
├── [继承] Clip C  ← 可以启用（如果在 Source 中被禁用）
├── Clip D         ← 变体自己的 Clip
└── Clip E         ← 变体自己的 Clip
```

### 特性

- **继承的 Clip 是只读的**：不能修改、删除或移动继承的 Clip
- **可以覆盖启用/禁用状态**：即使 Source 中某个 Clip 被禁用，变体可以重新启用它，反之亦然
- **帧数可以覆盖**：变体可以设置自己的帧数，设为 0 表示使用 Source 的帧数
- **递归继承**：变体也可以作为其他变体的 Source

### API

```csharp
// 设置变体关系
myAsset.SetSource(sourceAsset);

// 检查是否为变体
bool isVariant = myAsset.IsVariant;

// 获取 Source
ActionLineAsset source = myAsset.Source;

// 覆盖 Clip 的启用状态
myAsset.SetClipActive(inheritedClip, false); // 禁用继承的 Clip

// 获取含继承信息的 Clip 列表
List<ActionClipData> allClips = new List<ActionClipData>();
myAsset.ExportClipData(allClips);
// allClips 中的 IsInherit 字段标记是否为继承的 Clip
```

---

## 属性参考

### Clip 类型属性

| 属性 | 目标 | 说明 |
|------|------|------|
| `[ActionLineType(typeof(T))]` | `ActionLineClip` 子类 | 关联到指定的 Asset 类型，可多次使用 |
| `[ActionClipColor(r, g, b)]` | `ActionLineClip` 子类 | 设置 Clip 颜色栏（float 0-1 或 byte 0-255） |
| `[Alias("名称")]` | `ActionLineClip` 子类 | 设置在创建菜单中的显示名称 |

### 编辑器扩展属性

| 属性 | 目标 | 说明 |
|------|------|------|
| `[CustomClipEditor(typeof(ClipType))]` | `ActionClipEditor` 子类 | 为指定 Clip 类型注册自定义编辑器 |
| `[CustomClipPreview(typeof(ClipType))]` | `IClipPreview` 实现类 | 为指定 Clip 类型注册预览模拟器 |
| `[CustomEditorProvider(typeof(AssetType))]` | `ActionLineEditorProvider` 子类 | 为指定 Asset 类型注册编辑器提供者 |
| `[CustomEditorAction(typeof(AssetType))]` | `EditorAction` 子类 | 限定 Action 仅对指定 Asset 类型生效 |

### 字段装饰属性

| 属性 | 说明 |
|------|------|
| `[Combined]` | 将字段分组在一起显示 |
| `[Display("标签", "提示")]` | 自定义显示名称和鼠标悬停提示 |
| `[ReadOnly]` | 字段只读，不可编辑 |
| `[Multiline]` | 多行文本输入框 |
| `[PropertyMotion]` | 启用动画关键帧风格的控制 |

---

## 快捷键参考

### 内置快捷键

| 快捷键 | 操作 |
|--------|------|
| `Ctrl+N` | 创建新 Clip |
| `Ctrl+C` | 复制选中的 Clip |
| `Ctrl+V` | 粘贴 Clip |
| `Ctrl+D` | 复制并粘贴（Duplicate） |
| `Ctrl+A` | 全选 |
| `Delete` | 删除选中的 Clip |
| `Alt+按键` | 触发选中 Clip 的自定义快捷键（需 ClipEditor 支持） |

### 自定义快捷键

通过 `EditorAction` 的属性配置：

```csharp
public override KeyCode ShortCutKey => KeyCode.R;  // 按键
public override bool ActionKey => true;              // 需要 Ctrl/Cmd
public override bool ShiftKey => false;              // 需要 Shift
```

快捷键组合示例：
- `ShortCutKey = KeyCode.R, ActionKey = true` → `Ctrl+R`
- `ShortCutKey = KeyCode.R, ShiftKey = true` → `Shift+R`
- `ShortCutKey = KeyCode.R, ActionKey = true, ShiftKey = true` → `Ctrl+Shift+R`
- `ShortCutKey = KeyCode.F5` → `F5`

---

## API 参考

### ActionLineAsset

```csharp
public class ActionLineAsset : CollectableScriptableObject
{
    // 属性
    int FrameCount { get; }              // 总帧数（变体会继承 Source 的帧数）
    int SelfFrameCount { get; }          // 自身帧数
    bool IsVariant { get; }              // 是否为变体
    ActionLineAsset Source { get; }      // 变体的 Source Asset
    IReadOnlyList<ActionLineClip> Clips { get; }  // 自身的 Clip 列表

    // 方法
    void SetFrameCount(int count);                    // 设置帧数
    void SetSource(ActionLineAsset newSource);        // 设置变体关系
    void SetClipActive(ActionLineClip clip, bool active);  // 覆盖 Clip 启用状态
    bool ContainsClip(ActionLineClip clip);           // 递归检查是否包含 Clip
    bool IsClipActive(ActionLineClip clip);           // 递归检查 Clip 是否激活
    void ExportClipData(List<ActionClipData> datas);  // 导出含继承信息的 Clip 列表
    int AddClip(ActionLineClip clip);                 // 添加 Clip
    void RemoveClip(ActionLineClip clip);             // 移除 Clip
    int MoveToBehind(ActionLineClip clip, ActionLineClip target);  // 移动 Clip 排序
    void RegisterUndo(string name);                   // 注册撤销操作
}
```

### ActionLineClip

```csharp
public class ActionLineClip : ScriptableObject
{
    ActionLineAsset Owner;    // 所属 Asset
    bool Disable;             // 是否禁用
    string Description;       // 描述
    int StartFrame;           // 起始帧
    int Length;               // 帧长度（默认 1）
    bool Foldout { get; set; }  // 属性面板折叠状态（不序列化）
}
```

### ActionClipData

```csharp
public struct ActionClipData
{
    ActionLineClip Clip;   // Clip 引用
    bool IsInherit;        // 是否为继承的 Clip
    bool IsActive;         // 是否激活
}
```

### ActionLineEditorContext

```csharp
public class ActionLineEditorContext : ScriptableObject
{
    // 属性
    ActionLineAsset Target { get; }                  // 当前编辑的 Asset
    ActionLineView View { get; }                     // UI 视图
    IReadOnlyList<ActionClipData> Clips { get; }     // 所有 Clip 数据
    List<ActionClipData> SelectedClips;               // 选中的 Clip
    List<ActionClipData> SelectedTracks;              // 选中的轨道
    PreviewResourceContext ResourceContext { get; }    // 预览资源上下文
    bool IsPreviewEnable { get; }                     // 预览是否启用

    // 方法
    ActionLineView RequireView();                     // 获取或创建 UI 视图
    void SetTarget(ActionLineAsset asset);            // 设置编辑目标
    void CreatePreview();                             // 创建预览系统
    void DestroyPreview();                            // 销毁预览系统
    void RefreshView();                               // 刷新整个 UI
    void RefreshViewPort();                           // 恢复视口状态
    void RefreshSelectState();                        // 刷新选择高亮
    void RegisterUndo(string name, ActionLineClip clip = null);  // 注册撤销
    void ShowContextMenue(ActionModeType mode, int frameIndex);  // 显示右键菜单
    void ShowTypeSelectWindow();                      // 显示 Clip 类型选择窗口
    void Clear();                                     // 清理状态
}
```

### ActionLinePreviewContext

```csharp
public class ActionLinePreviewContext
{
    // 属性
    int FrameIndex { get; }                           // 当前预览帧
    ActionLineAsset Target { get; }                   // 预览的 Asset
    PreviewResourceContext ResourceContext { get; }    // 资源上下文

    // 方法
    void Refresh(List<ActionClipData> clips);         // 刷新 Clip 列表
    void SetFrame(int index);                         // 设置当前帧（触发 Start/Update/End）
    void DrawGizmos();                                // 绘制所有 Clip 的 Gizmos
    void Clear();                                     // 清理所有模拟器
    void Disable();                                   // 禁用预览

    // 可覆盖的方法
    virtual void OnCreate();                          // 创建时调用
    virtual void Disable();                           // 禁用时调用
    virtual void Destroy();                           // 销毁时调用
    virtual IClipPreview CreateSimulator(ActionLineClip clip);  // 创建 Clip 模拟器
    virtual void OnBeforFramePreview();               // 帧预览前回调
    virtual void OnAfterFramePreview();               // 帧预览后回调
}
```

---

## 扩展点总结

| 扩展点 | 基类/接口 | 属性 | 用途 |
|--------|-----------|------|------|
| 自定义 Clip | `ActionLineClip` | `[ActionLineType]` | 定义新的时间轴片段数据 |
| Clip 外观 | — | `[ActionClipColor]`, `[Alias]` | 设置颜色和显示名称 |
| Clip 编辑器 | `ActionClipEditor` | `[CustomClipEditor]` | 自定义 Clip 的 UI 和交互 |
| Clip 预览 | `TClipSimulator<T>` | `[CustomClipPreview]` | 定义编辑器内预览行为 |
| 右键菜单/快捷键 | `EditorAction` | `[CustomEditorAction]`（可选） | 添加自定义操作 |
| 编辑器提供者 | `ActionLineEditorProvider` | `[CustomEditorProvider]` | 定制整个编辑器体验 |
| 资源管理 | `PreviewResourceContext` | — | 管理预览所需的场景资源 |
