# GOAP 框架用户手册

## 一、概述

本框架提供一套完整的 **Goal-Oriented Action Planning（面向目标的行动规划）** 系统，适用于 Unity NPC AI。

**核心理念**：游戏代码只需描述"世界状态"和"行动能做什么"，AI 自动规划出代价最低的行动序列来达成目标。

**三层职责分离**：

| 层次 | 类 | 职责 |
|---|---|---|
| 目标选择 | `GoalSelector` | 从 Goal 列表中选出最高优先级的目标（含滞后防抖） |
| 规划搜索 | `Planner` | 反向 A\* 搜索最低代价行动序列 |
| 计划执行 | `PlanExecutor` | 驱动 Action 生命周期（Enter/Update/Exit/Abort） |

---

## 二、快速上手（五步接入）

### 第一步：定义世界状态键

用两个枚举分别承载 `bool` 类型和 `int` 类型的世界状态键。两个枚举的整数值**允许重叠**——框架通过高位掩码（bit30）自动区分 Bool 和 Int 键。

```csharp
public enum BoolKey
{
    HasWeapon    = 0,
    EnemyInSight = 1,
    EnemyInRange = 2,
}

public enum IntKey
{
    Health   = 0,
    AmmoLeft = 1,
}
```

### 第二步：定义执行 struct（`IActionData`）

每种行动类型对应一个 struct，**只存放类型专有字段**，规划数据（Cost/Preconditions/Effects）由编辑器配置。

```csharp
public struct MeleeAttackData : IActionData
{
    public string Id => "MeleeAttack";
    public float AttackRange;   // 类型专有字段
}
```

### 第三步：实现行动执行逻辑（`TActionRunner<T>`）

继承 `TActionRunner<T>`，重写需要的生命周期方法，做成无状态单例。

```csharp
public class MeleeAttackRunner : TActionRunner<MeleeAttackData>
{
    public static readonly MeleeAttackRunner Instance = new MeleeAttackRunner();

    protected override bool OnIsApplicable(WorldState current, MeleeAttackData data)
    {
        // 动态可行性：敌人在攻击范围内
        return current.GetBool((int)BoolKey.EnemyInRange);
    }

    protected override ActionStatus OnUpdate(AgentContext ctx, MeleeAttackData data, float dt)
    {
        // 播放攻击动画、扣血等逻辑
        return ActionStatus.Completed;
    }

    // 可选重写
    protected override void OnEnter(AgentContext ctx, MeleeAttackData data) { }
    protected override void OnExit(AgentContext ctx, MeleeAttackData data) { }
    protected override void OnAbort(AgentContext ctx, MeleeAttackData data) { }
}
```

### 第四步：在游戏启动时注册 Runner，并用 `AgentFactory` 创建 Agent

```csharp
void Awake()
{
    // 1. 注册所有 Runner（每种数据类型注册一次）
    ActionRunner<MeleeAttackData>.Runner = MeleeAttackRunner.Instance;

    // 2. 加载导出的运行时数据（JSON/ScriptableObject，自行序列化）
    GOAPRuntimeData runtimeData = LoadRuntimeData();

    // 3. 工厂构建 Agent（自动填充 Actions 和 Goals）
    _agent = AgentFactory.Create(runtimeData);
}
```

### 第五步：每帧驱动

```csharp
void Update()
{
    // 感知层：每帧更新世界状态（SetBool/SetInt 自动编码高位掩码）
    _agent.WorldState.SetBool((int)BoolKey.EnemyInSight, DetectEnemy());
    _agent.WorldState.SetInt((int)IntKey.Health, CurrentHealth);

    // 驱动 AI
    _agent.Tick(Time.deltaTime);
}
```

---

## 三、编辑器工作流

### 打开编辑器

菜单：`Tools → GOAP Editor`

### 创建 ConfigAsset

1. 继承 `ConfigAsset`，实现 `BoolKeyType` 和 `IntKeyType`：

```csharp
[GOAPTag(typeof(MeleeGroup), typeof(RangedGroup))]
[CreateAssetMenu(menuName = "GOAP/WarriorConfig")]
public class WarriorConfig : ConfigAsset
{
    public override Type BoolKeyType => typeof(BoolKey);
    public override Type IntKeyType  => typeof(IntKey);
}
```

2. 在 Project 窗口右键 → Create → GOAP → WarriorConfig，创建资产文件。

### 声明 Action 类型分组

```csharp
// 声明分组（Name = "Melee"）
public class MeleeGroup : ActionGroupAttribute { }

// 绑定到分组
[MeleeGroup]
public class MeleeActionData : TActionData<MeleeAttackData> { }
```

### 在编辑器中配置 Action / Goal

在 GOAP Editor 窗口中：

- **Actions 列**：点击"+ 添加"选择分组类型，配置 Cost、前置条件、效果
- **Goals 列**：点击"+ 添加"配置 BasePriority、期望终态
- 点击 **"保存/导出"** → 生成 `GOAPRuntimeData` 对象（调用方自行序列化）

### 定义 Goal 数据

```csharp
public struct KillEnemyData : IGoalData
{
    public string Id => "KillEnemy";
    public float BasePriority => 80f;
    public WorldState DesiredState { get; set; }  // 由编辑器填充
}

public class MeleeGroup : ActionGroupAttribute { }

// Goal 的编辑器资产类
public class KillEnemyGoalData : TGoalData<KillEnemyData> { }
```

---

## 四、关键概念详解

### WorldState

运行时世界状态，以整数键值对存储。键为枚举整数值，值统一用 `int`（bool 用 0/1）。

```csharp
// 推荐：使用 SetBool/SetInt（自动编码高位掩码）
agent.WorldState.SetBool((int)BoolKey.HasWeapon, true);
agent.WorldState.SetInt((int)IntKey.Health, 80);

bool hasWeapon = agent.WorldState.GetBool((int)BoolKey.HasWeapon);
int health = agent.WorldState.GetInt((int)IntKey.Health);
```

- **规划用途**：`Preconditions`/`Effects` 均为 `WorldState` 子集
- **感知层写入**：每帧由游戏代码根据实际情况更新
- **规划器只读克隆**：规划搜索时对状态做 `Clone()`，不污染实际状态

### ActionStatus（`OnUpdate` 的返回值）

| 值 | 含义 |
|---|---|
| `Running` | 行动进行中，下帧继续 `OnUpdate` |
| `Completed` | 行动成功，执行器推进到下一个 Action，并将 Effects 写入 WorldState |
| `Failed` | 行动失败，触发重规划 |

### AgentStatus

| 值 | 含义 |
|---|---|
| `Idle` | 无有效目标 |
| `Planning` | 正在规划（同步，当帧完成） |
| `Executing` | 正在执行计划 |
| `Failed` | 规划无解 |

### 目标滞后防抖（InsistenceBias）

防止优先级微小波动导致目标每帧频繁切换：

> 只有当新目标优先级 − 当前目标优先级 > `InsistenceBias` 时才发生切换

重写 `Goal<T>.OnGetInsistenceBias()` 调整阈值：

```csharp
protected override float OnGetInsistenceBias() => 10f;
```

---

## 五、扩展：自定义 Goal 动态逻辑

`BasicGoal` 使用静态 `BasePriority`，如需动态优先级，继承 `Goal<T>`：

```csharp
public class KillEnemyGoal : Goal<KillEnemyData>
{
    public KillEnemyGoal(KillEnemyData data) : base(data) { }

    // 视野内有敌人时才有效
    protected override bool OnIsValid(WorldState current)
        => current.GetBool((int)BoolKey.EnemyInSight);

    // 血量越低，击杀优先级越低
    protected override float OnGetPriority(WorldState current)
    {
        int hp = current.GetInt((int)IntKey.Health);
        return Data.BasePriority * (hp / 100f);
    }
}
```

在 `AgentFactory.Create()` 返回的 agent 上手动替换：

```csharp
agent.Goals.RemoveAll(g => g.Id == "KillEnemy");
agent.Goals.Add(new KillEnemyGoal(myKillEnemyData));
```

---

## 六、扩展：监听规划事件

继承 `Agent` 并重写虚方法：

```csharp
public class WarriorAgent : Agent
{
    protected override void OnGoalChanged(IGoal newGoal)
    {
        Debug.Log($"目标切换 → {newGoal.Id}");
    }

    protected override void OnPlanChanged(Plan plan)
    {
        if (!plan.IsValid)
            Debug.LogWarning("规划失败，无解");
        else
            Debug.Log($"新计划：{string.Join(" → ", plan.Actions.Select(a => a.Id))}");
    }
}
```

---

## 七、规划器注意事项

- 规划器采用**反向 A\***：从目标终态出发，反推前置条件，直到当前状态满足所有条件
- 启发函数为"未满足条件数量"，**仅当所有 Action.Cost ≥ 1 时保证最优解**；Cost < 1 时仅保证找到解，不保证最优
- `maxDepth`（默认 10）限制搜索深度，防止死循环；复杂场景可适当增大
- 规划是**同步操作**，在 `Tick()` 的当帧内完成；Action 数量极多时需注意性能

---

## 八、文件结构速查

```
GOAP/
  Runtime/
    Agent/
      IAction.cs          — 行动接口（规划数据 + 执行生命周期）
      IActionData.cs      — 执行 struct 标记接口（仅含 Id）
      TActionExecutor.cs  — TActionRunner<T> 泛型基类（子类重写点）
      ActionExecutor.cs   — ActionRunner<T> 静态注册槽
      Action.cs           — RuntimeAction<T>（持有规划数据 + 分发 Runner）
      Agent.cs            — AI 主驱动器
      AgentContext.cs     — 传入 Action 生命周期的上下文（readonly struct）
      GoalSelector.cs     — 目标选择器（含滞后防抖）
      PlanExecutor.cs     — 计划执行器（管理生命周期保证）
      IGoal.cs / Goal.cs  — 目标接口与泛型基类
      BasicGoal.cs        — 静态优先级目标（由工厂直接构造）
    Data/
      RuntimeData.cs      — 所有运行时数据结构（GOAPRuntimeData 等）
      WorldState.cs       — 世界状态容器（排序数组，定长 32）
      Plan.cs             — 规划结果容器
    Planner/
      Planner.cs          — 反向 A* 规划器（静态，对象池化）
      MinHeap.cs          — 最小堆（Planner 内部使用）
    Factory/
      AgentFactory.cs     — 从 GOAPRuntimeData 构造 Agent
  Editor/
    Asset/
      ConfigAsset.cs      — 配置资产基类（ScriptableObject）
      ActionData.cs       — Action 编辑器数据基类
      TActionData.cs      — 泛型 Action 编辑器模板
      GoalData.cs         — Goal 编辑器数据基类
      TGoalData.cs        — 泛型 Goal 编辑器模板
      ActionGroupAttribute.cs — 分组与 GOAPTag Attribute 定义
    Export/
      Exporter.cs         — 将 ConfigAsset 导出为 GOAPRuntimeData
    Window/
      GOAPWindow.cs       — 编辑器主窗口（Tools/GOAP Editor）
```
