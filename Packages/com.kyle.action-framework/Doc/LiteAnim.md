# LiteAnim 用户手册

## 概述

LiteAnim 是基于 Unity **PlayableGraph** API 的轻量级动画图框架，提供动画分层、混合与状态过渡能力，作为对 Unity Animator Controller 的轻量替代。

**主要特性：**
- 基于 PlayableGraph，零 Animator Controller 依赖
- 支持多层动画（分层叠加/覆盖，AvatarMask 遮罩）
- 两种 Motion 类型：单/拼接片段（Clip）和混合树（BlendTree）
- 带融合时长的状态过渡，支持逆向融合
- 可覆写的每对 Motion 融合时长（FadeOverride）
- 编辑器内实时预览

**目录结构：**
```
LiteAnim/
├── Runtime/
│   ├── Asset/       # 数据资产定义
│   ├── Player/      # PlayableGraph 封装与控制器
│   └── State/       # 动画状态实现
└── Editor/
    ├── View/        # 编辑器 UI 组件
    ├── Preview/     # 编辑器内预览
    └── Event/       # 编辑器事件
```

---

## 目录

1. [核心概念](#核心概念)
2. [数据资产](#数据资产)
3. [运行时接入](#运行时接入)
4. [Motion 类型详解](#motion-类型详解)
5. [控制器类型](#控制器类型)
6. [BlendTree 参数](#blendtree-参数)
7. [编辑器使用](#编辑器使用)
8. [API 参考](#api-参考)

---

## 核心概念

```
LiteAnimAsset（资产）
├── LiteAnimLayer × N     分层定义（叠加/遮罩）
├── LiteAnimMotion × N    动画状态定义
│   └── MotionClip × N   片段列表
└── MotionFadeOverride × N  自定义融合时长

LiteAnimGraph（PlayableGraph 封装）
└── AnimController（控制器，驱动状态机）
    └── MotionState × N   运行时动画状态
```

**两种控制器模式**：
- `MixableController` — 单层，同时只有一个 Motion 处于融合中，适合角色主动画层
- `LayerableController` — 多层，每层独立播放和过渡，底层通过 `AnimationLayerMixerPlayable` 叠合

---

## 数据资产

### LiteAnimAsset

通过菜单 **Create → LitAnim → LiteAnimAsset** 创建。

| 字段 | 类型 | 说明 |
|------|------|------|
| `DefaultFadeDuration` | `float` | 默认过渡融合时长（秒），全局默认值 |
| `Motions` | `List<LiteAnimMotion>` | 该资产包含的所有动画状态 |
| `Layers` | `List<LiteAnimLayer>` | 分层定义（仅 `LayerableController` 使用） |
| `FadeOverrides` | `List<MotionFadeOverride>` | 特定 Motion 对之间的自定义融合时长 |

### LiteAnimLayer

| 字段 | 说明 |
|------|------|
| `LayerName` | 层名称（仅描述用） |
| `Additive` | 是否叠加模式（`true` = 叠加，`false` = 覆盖） |
| `Mask` | `AvatarMask`，为空则影响全身 |

### LiteAnimMotion

每个 Motion 是一个独立的 `ScriptableObject`，存储在 `LiteAnimAsset` 的 `Motions` 列表中。

| 字段 | 说明 |
|------|------|
| `Type` | `MotionType.Clip`（单/拼接片段）或 `MotionType.BlendTree`（混合树） |
| `Loop` | 是否循环播放 |
| `LayerIndex` | 所属层索引（仅 `LayerableController` 中有效） |
| `Clips` | `List<MotionClip>`，片段列表 |
| `Param` | BlendTree 的驱动参数名（仅 `BlendTree` 类型使用） |

### MotionClip

| 字段 | 类型 | 说明 |
|------|------|------|
| `Asset` | `AnimationClip` | 动画片段资源 |
| `Speed` | `float` | 播放速度倍率（默认 1） |
| `StartOffset` | `float` | 从 `Asset` 的第几秒开始采样（秒） |
| `EndOffset` | `float` | 在 `Asset` 末尾第几秒停止采样（秒） |
| `MixIn` | `float [0,1]` | 仅拼接片段（Clip 多段）：与上一段的混叠时间百分比，`混叠时长 = MixIn × Length` |
| `Weight` | `float` | 仅 BlendTree：该片段的权重区间宽度 |

### MotionFadeOverride

| 字段 | 说明 |
|------|------|
| `From` | 源 Motion |
| `To` | 目标 Motion |
| `FadeDuration` | 此对之间的自定义融合时长（秒），优先于 `DefaultFadeDuration` |

---

## 运行时接入

### 基础接入（MixableController）

```csharp
using LiteAnim;
using UnityEngine;

public class CharacterAnim : MonoBehaviour, ILiteAnimPlayer
{
    [SerializeField] private LiteAnimAsset asset;
    [SerializeField] private Animator animator;

    private LiteAnimGraph graph;
    private MixableController controller;
    private BlendParam blendParam = new BlendParam();

    void Start()
    {
        // 1. 创建 PlayableGraph
        graph = LiteAnimGraph.Create("CharacterAnim", animator);

        // 2. 创建控制器并初始化
        controller = new MixableController();
        controller.Init(asset, graph, this);

        // 3. 播放初始状态
        controller.Play("Idle");

        // 4. 启动 PlayableGraph
        graph.Graph.Play();
    }

    void Update()
    {
        // 驱动融合参数（供 BlendTree 使用）
        blendParam.SetParam("MoveSpeed", GetMoveSpeed());

        // 驱动控制器更新（推进融合时间）
        controller.Update(Time.deltaTime);

        // 手动推进 PlayableGraph 采样（也可通过 UpdateMode 自动驱动）
        graph.Graph.Evaluate(Time.deltaTime);
    }

    // ILiteAnimPlayer 实现
    public float GetParam(string name) => blendParam.GetParam(name);

    void OnDestroy()
    {
        controller?.Destroy();
        graph?.Destroy();
    }
}
```

### 多层接入（LayerableController）

```csharp
// 资产中需配置 Layers（如 Base Layer + Upper Body Layer）
controller = new LayerableController();
controller.Init(asset, graph, this);

// 播放时自动按 Motion.LayerIndex 分层
controller.Play("Run");          // LayerIndex = 0，影响全身
controller.Play("AimUpperBody"); // LayerIndex = 1，仅上半身（AvatarMask）

// 停止指定层
controller.StopLayer(1);
```

### 更新模式

```csharp
// 三种 PlayableGraph 更新模式
graph.Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);          // 默认，跟随 Time.deltaTime
graph.Graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);  // 不受 timeScale 影响
graph.Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);            // 手动调用 Evaluate
```

---

## Motion 类型详解

### Clip 类型 — 单片段

`Clips` 列表中只有一个有效 `AnimationClip`，直接播放。

```
Motion (Clip)
└── Clips[0]: Asset=Walk.anim, Speed=1, StartOffset=0
```

### Clip 类型 — 拼接片段（SpliceClip）

`Clips` 列表有多个有效片段，按顺序拼接播放，相邻片段间通过 `MixIn` 交叉融合。

```
Motion (Clip, 多段)
├── Clips[0]: Asset=AttackA.anim, MixIn=0
├── Clips[1]: Asset=AttackB.anim, MixIn=0.2  ← 与 A 末尾融合 20%
└── Clips[2]: Asset=AttackC.anim, MixIn=0.15
```

总时长 = 各段有效时长之和（减去融合重叠部分）。
运行时 `LiteAnimMotion.GetLength()` 与拼接状态的实际时间轴一致，会扣除每段 `MixIn` 造成的重叠时长。

### BlendTree 类型

按 `Param` 参数值在多个 `AnimationClip` 之间线性融合，使用圆形权重布局：片段按 `Weight` 字段比例分配参数轴区间，参数值落在哪个区间就融合对应的相邻两个片段。

```
Motion (BlendTree, Param="MoveSpeed")
├── Clips[0]: Asset=Idle.anim,    Weight=0.3  ← 参数 0.0 附近
├── Clips[1]: Asset=Walk.anim,    Weight=0.4  ← 参数 0.4 附近
└── Clips[2]: Asset=Run.anim,     Weight=0.3  ← 参数 0.8 附近
```

---

## 控制器类型

### MixableController

- 同一时刻最多一个融合过渡（From → To）
- 调用 `Play(name)` 时：
  - 若目标正在融合中 → 反向融合
  - 若已是当前状态 → 忽略
  - 否则 → 创建新过渡，From 结束后销毁其 Playable

### LayerableController

- 每层独立维护融合状态，互不干扰
- 底层使用 `AnimationLayerMixerPlayable`，支持叠加层和 AvatarMask 遮罩
- `StopLayer(layerIndex)` — 淡出指定层
- 叠加层的非循环片段播放完成后会断开该层；如果同层仍在过渡中，会等过渡结束后再处理，避免误断目标状态

### 融合流程

1. `Play(name)` → 找到对应 MotionState，从资产查询 FadeDuration
2. 将当前状态作为 From，新状态作为 To，创建 `TransitionInfo`
3. 每帧 `Update(dt)` 推进 `FadeTime`，调整 From/To 权重
4. `FadeTime >= FadeDuration` → 过渡完成，销毁 From 的 Playable

---

## BlendTree 参数

`BlendParam` 是一个可序列化的具名浮点参数容器，同时实现 `ILiteAnimPlayer`：

```csharp
var param = new BlendParam();
param.SetParam("MoveSpeed", 3.5f);
param.SetParam("Direction",  0.2f);

float v = param.GetParam("MoveSpeed"); // 3.5f
```

直接将 `BlendParam` 实例作为 `ILiteAnimPlayer` 传入 `controller.Init(asset, graph, blendParam)` 即可，无需额外实现接口。

---

## 编辑器使用

### 打开编辑器

菜单 **Tools → LiteAnim → LiteAnim Editor**，或双击 `LiteAnimAsset` 资产文件。

### 界面布局

编辑器分三个 Tab：

| Tab | 说明 |
|-----|------|
| **Motions** | Motion 列表 + 选中 Motion 的详情编辑（片段、参数配置） |
| **Properties** | 资产全局属性（DefaultFadeDuration、Layers 列表） |
| **FadeOverrides** | 自定义特定 Motion 对的融合时长 |

### 预览

1. 在 **Properties** Tab 指定预览模型（含 Animator 的 Prefab）
2. 勾选 **Preview** Toggle 开启预览，模型将实例化到当前 Scene
3. 在 **Motions** Tab 选中 Motion，编辑器实时在场景中播放该 Motion
4. 关闭 Preview Toggle 或窗口后模型自动销毁（`HideFlags.DontSave`）

### Motion 编辑

- **Clip 类型**：在详情面板拖入 `AnimationClip`，调整 Speed / StartOffset / EndOffset / MixIn
- **BlendTree 类型**：添加多个片段，设置各片段 Weight 和 Param 参数名；预览时可通过 `AnimParamValueChangedEvent` 实时调整参数观察融合效果

---

## API 参考

### LiteAnimGraph

| 成员 | 说明 |
|------|------|
| `static Create(name, animator)` | 创建并初始化 PlayableGraph 和根混合器 |
| `Graph` | 底层 `PlayableGraph` |
| `Target` | 目标 `Animator` |
| `ConnectToRoot<V>(playable, weight)` | 将 Playable 接入根混合器，返回插槽索引 |
| `GetEmptyRootIndex()` | 获取空闲的根混合器插槽索引 |
| `SetRootWeight(index, weight)` | 设置指定插槽的权重 |

### AnimController

| 成员 | 说明 |
|------|------|
| `Init(asset, graph, player)` | 初始化控制器 |
| `Play(name)` | 按名称切换到指定 Motion，自动触发融合过渡 |
| `StopLayer(layerIndex)` | 停止指定层（`LayerableController` 专有） |
| `Update(dt)` | 推进融合时间，需每帧调用 |
| `SetWeight(weight)` | 设置该控制器在根混合器中的权重 |
| `Destroy()` | 销毁控制器状态，并释放其在 PlayableGraph 中占用的连接和混合器 |
| `Playing` | 当前正在播放的 `StatePlayInfo` 列表 |
| `Transitions` | 当前进行中的 `TransitionInfo` 列表 |

### LiteAnimUtil

| 方法 | 说明 |
|------|------|
| `CreateState(motion)` | 根据 `LiteAnimMotion.Type` 创建对应的 `MotionState` 实例 |
| `ToDirectorMode(updateMode)` | `MontageUpdateMode` → `DirectorUpdateMode` 转换 |
