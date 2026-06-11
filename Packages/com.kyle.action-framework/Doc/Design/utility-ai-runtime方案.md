# UtilityAI Runtime 方案草案

## 设计边界

Runtime 是一个嵌入业务模块使用的决策框架，不独立运行。

Runtime 不耦合距离、目标、技能、移动、动画等业务逻辑。业务层负责定义 `TContext`、Action 配置、Handler 实现，以及最终执行行为。

## Action 配置

Action 配置是纯 C# POD 类型，由 Unity 业务层从 `ScriptableObject` 或其它配置源转换而来。

用户可配置数据使用字段。只有由子类写死的类型信息使用属性。

```csharp
public abstract class UtilityActionConfig
{
    public string ActionId;
    public abstract string ActionType { get; }
}

public abstract class TerminalActionConfig : UtilityActionConfig
{
}

public abstract class SupportActionConfig : UtilityActionConfig
{
    public UtilitySupportChannel[] Channels;
    public int MinHoldTicks;
}
```

`ActionId` 可为空。为空时 runtime debug/log 使用 `{ActionType}[index]` 作为 fallback。

`ActionType` 只用于 debug、log、editor 显示和配置错误提示，不作为 Handler 匹配主键。

Handler 匹配主键是 `config.GetType()`。

## AI 配置

```csharp
public sealed class UtilityAiConfig
{
    public TerminalActionConfig[] TerminalActions;
    public SupportActionConfig[] SupportActions;

    public int MaxSupportCount;
    public float RepositionMargin;
    public float IntentStickiness;
    public int PreparationTimeoutTicks;
}
```

数组顺序就是优先级。index 越小，优先级越高。

`SupportActions == null` 按空数组处理。`TerminalActions` 不能为空。

## Handler

Handler 是全局单例，无运行时状态。

同类型不同参数的多个 Action 表现为多个 Config 实例，共用同一个 Handler 实例。

```csharp
public abstract class TerminalActionHandler<TContext, TConfig>
    where TConfig : TerminalActionConfig
{
    public abstract TerminalScore Score(
        TContext context,
        TConfig config,
        TerminalScoreInput input);

    public abstract UtilityIntentBuildStatus BuildIntent(
        TContext context,
        TConfig config,
        TerminalIntentBuildInput input);

    public abstract UtilityActionExecutionStatus Execute(
        TContext context,
        TConfig config,
        TerminalExecuteInput input);

    public abstract bool CanBeInterrupted(
        TContext context,
        TConfig config,
        TerminalInterruptInput input);

    public abstract bool IsCommitFinished(
        TContext context,
        TConfig config,
        TerminalCommitInput input);
}
```

```csharp
public abstract class SupportActionHandler<TContext, TConfig>
    where TConfig : SupportActionConfig
{
    public abstract SupportScore Score(
        TContext context,
        TConfig config,
        SupportScoreInput input);

    public abstract UtilityIntentBuildStatus BuildIntent(
        TContext context,
        TConfig config,
        SupportIntentBuildInput input);

    public abstract UtilityActionExecutionStatus Execute(
        TContext context,
        TConfig config,
        SupportExecuteInput input);
}
```

`Score` 不写共享意图数据。

`BuildIntent` 可以写 `TContext` 中的强类型意图数据。

`Execute` 调用业务模块完成实际行为。

单个 Action 的执行状态：

```csharp
public enum UtilityActionExecutionStatus
{
    Success,
    Failed,
    Rejected
}
```

## TContext 约束

同一个 `TContext` 下，所有 Action 共享同一套强类型决策参数。

Runtime 不提供 blackboard，也不引入 `TBlackboard`。

`TContext` 需要实现：

```csharp
public interface IUtilityIntentState
{
    void ResetUtilityIntentState();
}

public interface IUtilitySupportConstraintProvider
{
    bool IsSupportRequired(SupportActionConfig config, int supportIndex);
    bool IsSupportAllowed(SupportActionConfig config, int supportIndex);
    bool IsSupportForbidden(SupportActionConfig config, int supportIndex);
}
```

Runtime 泛型约束：

```csharp
public sealed class UtilityAiRuntime<TContext>
    where TContext : IUtilityIntentState, IUtilitySupportConstraintProvider
{
}
```

`ResetUtilityIntentState` 会在 Evaluate 开始，以及尝试新的 Terminal 候选前调用。

## Runtime 创建

业务层手动注册 Handler。

```csharp
public sealed class UtilityActionRegistry<TContext>
{
    public void Register<TConfig>(
        TerminalActionHandler<TContext, TConfig> handler)
        where TConfig : TerminalActionConfig;

    public void Register<TConfig>(
        SupportActionHandler<TContext, TConfig> handler)
        where TConfig : SupportActionConfig;
}
```

Registry 注册错误直接抛异常：

- `handler == null`：`ArgumentNullException`
- 同一个 `TConfig` 重复注册：`InvalidOperationException`

Terminal 与 Support 注册表彼此独立。

创建 runtime 时解析并缓存 Handler，不在 Tick 中查字典。

Registry 在 runtime 创建后仍可继续注册，但只影响之后创建的新 runtime，不影响已经创建好的 runtime。

```csharp
public sealed class UtilityAiRuntime<TContext>
{
    public readonly UtilityAiConfig Config;
    public readonly TerminalActionRuntime<TContext>[] TerminalActions;
    public readonly SupportActionRuntime<TContext>[] SupportActions;
}
```

```csharp
public struct TerminalActionRuntime<TContext>
{
    public TerminalActionConfig Config;
    public ITerminalActionInvoker<TContext> Handler;
    public TerminalScore LastScore;
    public int LastSelectedTick;
}

public struct SupportActionRuntime<TContext>
{
    public SupportActionConfig Config;
    public ISupportActionInvoker<TContext> Handler;
    public SupportScore LastScore;
    public bool IsActive;
    public int MinHoldUntilTick;
}
```

创建使用极简 `TryCreate`。配置非法、缺 Handler、`config == null` 或 `registry == null` 都返回 `false`，不抛异常、不返回错误列表、不打日志。

```csharp
public static bool TryCreate<TContext>(
    UtilityAiConfig config,
    UtilityActionRegistry<TContext> registry,
    out UtilityAiRuntime<TContext> runtime)
    where TContext : IUtilityIntentState, IUtilitySupportConstraintProvider;
```

## 创建校验

- `config == null`：失败
- `TerminalActions == null` 或长度为 0：失败
- `TerminalActions` 含 null：失败
- `SupportActions == null`：按空数组处理
- `SupportActions` 含 null：失败
- `SupportActionConfig.Channels == null` 或长度为 0：失败
- `Channels` 内有重复 channel：失败
- `MinHoldTicks < 0`：失败
- `MaxSupportCount < 0`：失败
- `RepositionMargin < 0`：失败
- `IntentStickiness < 0`：失败
- `PreparationTimeoutTicks < 0`：失败
- 找不到对应 Handler：失败

`MaxSupportCount == 0` 允许。

`PreparationTimeoutTicks == 0` 表示不启用准备超时惩罚。

## Tick API

Runtime 不读取 Unity 时间。时间由业务传入。

```csharp
public struct UtilityTickInfo
{
    public float Time;
    public float DeltaTime;
    public int TickIndex;
}
```

Evaluate 和 Execute 分离。

```csharp
public UtilityDecisionResult Evaluate(
    TContext context,
    UtilityTickInfo tickInfo);

public UtilityExecutionResult Execute(
    TContext context,
    UtilityDecisionResult decision,
    UtilityTickInfo tickInfo);
```

Evaluate 后更新决策记忆。

Execute 后更新已执行记忆。

Evaluate 成功后，`TContext` 保留本次已构建好的 intent/support 数据，供 Execute 使用。Evaluate 与 Execute 之间如果业务修改 `TContext`，由调用方负责一致性。

整体执行结果：

```csharp
public enum UtilityExecutionStatus
{
    Success,
    Failed,
    Rejected,
    PartialFailed
}

public struct UtilityExecutionResult
{
    public UtilityExecutionStatus Status;
}
```

## DecisionVersion

`UtilityDecisionResult` 不暴露 support index 数组所有权。

```csharp
public struct UtilityDecisionResult
{
    public int DecisionVersion;
    public UtilityIntentMode Mode;
    public int SelectedTerminalIndex;
    public int SelectedSupportCount;
}
```

Runtime 内部保存当前 decision 的选择 buffer。

```csharp
public int GetSelectedSupportIndex(int slot);
```

Execute 校验：

- `decision.DecisionVersion == runtime.CurrentDecisionVersion`
- `decision.DecisionVersion != runtime.LastExecutedDecisionVersion`

不满足则返回 Rejected。

同一个 decision 不允许重复 Execute。

每次 Evaluate 都自增 `DecisionVersion`，包括 `Mode=None`。

`Mode=None` 执行时不调用任何 Handler，标记该 `DecisionVersion` 已执行，并返回 `Success`。

## Intent Mode

```csharp
public enum UtilityIntentMode
{
    None,
    Execute,
    Prepare,
    Committed,
    Emergency
}
```

`None`：没有 Terminal 或 PreparationTarget，不评估、不构建、不执行 Support。

`Execute`：执行 Terminal，同时执行选中的 Support。

`Prepare`：不执行 Terminal，只执行 Support。

`Committed`：当前 Terminal 锁定中，不重新选择普通 Terminal，刷新当前 Terminal intent 并重新评估 Support。

`Emergency`：紧急 Terminal，可打断当前 committed terminal。

Support 不允许脱离 Terminal 或 PreparationTarget 独立存在。

## Score 数据

```csharp
public struct TerminalScore
{
    public bool CanExecute;
    public bool CanPrepare;

    public float ExecutionScore;
    public float PreparationScore;

    public bool IsEmergency;
    public float EmergencyScore;

    public int LockTicks;
    public bool CanInterruptCommitted;
}
```

```csharp
public struct SupportScore
{
    public bool IsValid;
    public float Score;
    public bool IsRequiredSatisfied;
}
```

Score 中不放距离、方向、目标点等业务数据。

这些数据由被选中的 Action 在 `BuildIntent` 阶段写入 `TContext`。

## Evaluate 流程

```text
1. ResetUtilityIntentState
2. 如果存在 committed terminal：
   - 调用 IsCommitFinished
   - 如果已结束，清除 committed，进入普通 Terminal 选择
   - 如果未结束，调用 CanBeInterrupted
   - 可打断时评估 Emergency
   - Emergency 失败或不可打断时，继续 Committed
3. Terminal.Score 全部候选
4. 根据 Execute / Prepare / Emergency 规则排序候选
5. 逐个尝试候选：
   - ResetUtilityIntentState
   - Terminal.BuildIntent
   - 读取 TContext 的 support required/allowed/forbidden 约束
   - Support.Score
   - Support 仲裁
   - Selected Support.BuildIntent
6. 必需环节失败则尝试下一个 Terminal 候选
7. 全部失败则 Mode=None
```

Committed 状态下不调用当前 committed terminal 的 `Score`。Committed 的 support 约束仍由当前 committed terminal 的 `BuildIntent` 写入 `TContext`。

Committed 失效条件：

- `IsCommitFinished == true`
- committed terminal `BuildIntent == Failed`
- required support `Score.IsRequiredSatisfied == false`
- required support `BuildIntent == Failed`

Committed 失效后清除 committed，重置 intent state，并进入普通 Terminal 选择。

Emergency 也走完整候选尝试流程：`BuildIntent`、required support 校验、Support 仲裁和 selected Support `BuildIntent` 都必须成功。单个 Emergency 失败时尝试下一个 Emergency。所有 Emergency 失败时，Committed 状态回到 Committed，非 Committed 状态继续普通候选。

Emergency 选中时，Evaluate 只记录将打断 committed，不在 Evaluate 阶段清除 committed。实际清除发生在 Execute 阶段。

`RepositionMargin` 规则：

```text
PreparationScore > ExecutionScore + RepositionMargin
=> Prepare

否则只要 CanExecute
=> Execute
```

相等时选择 Execute。

`IntentStickiness` 只作用于 Terminal 候选。上一 tick 选中的 Terminal 再次参与候选时加分。

`PreparationTimeoutTicks` 只抑制继续 Prepare，不影响 Execute。

Terminal 普通候选生成规则：

```text
if CanExecute && CanPrepare:
    if PreparationScore > ExecutionScore + RepositionMargin:
        mode = Prepare
        score = PreparationScore
    else:
        mode = Execute
        score = ExecutionScore
else if CanExecute:
    mode = Execute
    score = ExecutionScore
else if CanPrepare:
    mode = Prepare
    score = PreparationScore
```

同一个 Terminal 最多生成一个普通候选。Emergency 候选单独生成。

Terminal 普通候选排序：

```text
effectiveScore 高优先
effectiveScore 相同 index 小优先
```

`IntentStickiness` 加到上一 tick 选中的 Terminal 的普通候选分数上。Emergency 不受 `IntentStickiness`、`PreparationTimeoutTicks`、`RepositionMargin` 影响。

Emergency 排序：

```text
EmergencyScore 高优先
EmergencyScore 相同 index 小优先
```

分数规则：

- runtime 不限制分数范围
- 分数越高越优
- 负分允许
- `NaN` / `Infinity` 视为对应候选 invalid
- flag 为 false 时，对应 score 忽略
- 第一版不提供 diagnostics 或 log callback

## Support 仲裁

```csharp
public enum UtilitySupportChannel
{
    Movement,
    Facing,
    Positioning,
    Validation,
    Modifier
}
```

默认 channel 策略：

- `Movement`：SingleOwner
- `Facing`：SingleOwner
- `Positioning`：SingleOwner
- `Validation`：MultiOwner
- `Modifier`：MultiOwner

Support 仲裁优先级：

```text
required > min hold > score > lower index
```

选择时看评分，执行顺序看配置顺序。

所有 selected support 总数统一受 `MaxSupportCount` 限制。Required support 也受该限制，required 数量超过 `MaxSupportCount` 时当前 Terminal intent 失败。

一个 Support 占多个 channel 时，必须同时赢得它占用的所有 SingleOwner channel。MultiOwner channel 不需要独占。任一 SingleOwner channel 输掉，则该 Support 不选。

多个 required support 争同一个 SingleOwner channel 时，index 小的先占，后面的 required 无法满足，当前 Terminal intent 失败。Allowed support 与 required 冲突时，required 优先，allowed 被丢弃。

MinHold 只提高仲裁优先级，不绕过 forbidden、不绕过 allowed/required 约束、不绕过 score 有效性判断。

Required support 失败：

- `Score.IsRequiredSatisfied == false`：当前 Terminal intent 失败
- `BuildIntent == Failed`：当前 Terminal intent 失败

Allowed support 失败：

- `BuildIntent == Failed`：丢弃该 Support，继续其它 Support

Support `BuildIntent` 按最终仲裁顺序调用。后一个 Support 可以读取前一个 Support 写入 `TContext` 的强类型数据。

Support `BuildIntent` 和 `Execute` 顺序：

```text
required first -> allowed second -> lower index
```

## Execute 流程

Execute / Emergency：

```text
1. Terminal.Execute
2. Terminal Success 后调用 Support.Execute
3. Terminal Failed / Rejected 时停止，不调用 Support
```

Prepare / Committed：

```text
1. 不调用 Terminal.Execute
2. 调用 Support.Execute
```

Terminal Execute 返回 `Failed`：

- 记录失败
- 不再执行 Support
- 记录该 DecisionVersion 已执行
- 下一 tick 重新 Evaluate

Terminal Execute 返回 `Rejected`：

- 记录 Rejected
- 不再执行 Support
- 记录该 DecisionVersion 已执行
- 下一 tick 重新 Evaluate

Support Execute 失败：

- 记录该 Support 状态
- 不影响其它 Support Execute
- 整体执行结果为 `PartialFailed`

Support Execute 返回 `Rejected` 时也只让整体结果变成 `PartialFailed`，不阻断其它 Support。

`Terminal.Execute Success` 后：

- `LastScore.LockTicks > 0`：进入 Committed
- `LastScore.LockTicks <= 0`：不进入 Committed

`Emergency.Execute Success` 后同样按 `LockTicks` 决定是否进入新的 Committed。

Emergency 执行阶段会先清旧 committed。Emergency 执行失败或被拒绝时，不恢复旧 committed，下一 tick 重新 Evaluate。

Committed 是否结束由当前 Terminal Handler 的 `IsCommitFinished` 判断。

当前 committed terminal 是否可被 Emergency 打断，由当前 Terminal Handler 的 `CanBeInterrupted` 判断。
