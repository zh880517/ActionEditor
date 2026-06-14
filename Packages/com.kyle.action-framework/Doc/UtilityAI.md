# UtilityAI Runtime 手册

UtilityAI 是一个纯运行时决策框架，用于在业务模块中组合“主动作”和“支援动作”。Runtime 不内置距离、目标、技能、移动或动画逻辑；业务层负责定义 `TContext`、Action 配置和 Handler，并在 Handler 中完成具体评分、意图写入与执行。

## 核心概念

- `TerminalActionConfig` 表示主动作，例如攻击、施法、交互。每次决策最多选择一个 Terminal。
- `SupportActionConfig` 表示支援动作，例如移动、朝向、位置校验或效果修饰。Support 必须依附于当前 Terminal 或准备目标。
- `ActionId` 可为空，`ActionType` 只用于调试、日志和编辑器显示。Handler 匹配使用 `config.GetType()`。
- `UtilityAIConfig` 中的数组顺序就是优先级；分数相同或 required 冲突时，index 小的配置优先。

## 上下文约束

业务上下文需要同时实现：

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

Runtime 会在每次 `Evaluate` 开始，以及尝试新的 Terminal 候选前调用 `ResetUtilityIntentState`。Terminal 和 Support 的 `BuildIntent` 可以向 `TContext` 写入强类型意图数据，供后续 Support 或 `Execute` 使用。

## 注册与创建

业务层通过 `UtilityActionRegistry<TContext>` 注册无状态 Handler：

```csharp
var registry = new UtilityActionRegistry<MyContext>();
registry.Register(new AttackHandler());
registry.Register(new MoveSupportHandler());

if (!UtilityAIRuntime.TryCreate(config, registry, out UtilityAIRuntime<MyContext> runtime))
{
    // 配置非法或缺少 Handler。
}
```

`TryCreate` 不抛配置错误、不打日志。它会校验 Terminal 非空、Support channel 合法、数值非负，并把 Handler 缓存在 `TerminalActionRuntime` / `SupportActionRuntime` 中，后续 Tick 不再查询注册表。Runtime 创建之后继续修改 registry，只影响之后创建的新 Runtime。

## Tick 流程

调用方每帧传入时间信息：

```csharp
var decision = runtime.Evaluate(context, tickInfo);
var result = runtime.Execute(context, decision, tickInfo);
```

`Evaluate` 每次都会递增 `DecisionVersion`，包括 `Mode.None`。`Execute` 只接受当前版本且未执行过的 decision；同一个 decision 重复执行会返回 `Rejected`。`UtilityDecisionResult` 不暴露 Support 数组，调用方可用 `GetSelectedSupportIndex(slot)` 查看当前选择。

Runtime 内部状态按职责分组保存：`UtilityAISelectionBuffer` 负责候选列表和 Support 选择缓冲，`UtilityAIDecisionState` 负责当前决策版本和快照，`UtilityAICommitState` 负责 committed Terminal 的锁定信息，`UtilityAINormalSelectionState` 负责普通选择的粘性和准备超时记忆。这些类型是 `internal struct`，便于后续拆分到独立文件。

## Terminal 选择

Terminal 的 `Score` 可以同时给出执行、准备和紧急分数：

- 普通候选按有效分从高到低排序，分数相同时 index 小的优先。
- `PreparationScore > ExecutionScore + RepositionMargin` 时选择 `Prepare`，否则只要可执行就选择 `Execute`。
- `IntentStickiness` 只加到上一 tick 已选中的普通 Terminal 候选。
- `PreparationTimeoutTicks` 只抑制持续准备，不影响执行候选。
- `NaN` 和 `Infinity` 会让对应候选无效；负分允许。

Terminal 候选会先执行 `BuildIntent`。如果 Terminal 构建失败、必需 Support 失败，Runtime 会重置 intent 并尝试下一个 Terminal。

## Support 仲裁

Support channel 策略如下：

| Channel | 策略 |
| --- | --- |
| `Movement` | 单占用者 |
| `Facing` | 单占用者 |
| `Positioning` | 单占用者 |
| `Validation` | 多占用者 |
| `Modifier` | 多占用者 |

Support 选择优先级为 required、min hold、score、低 index。所有被选中的 Support 都计入 `MaxSupportCount`，required 也不例外。required 数量超过上限、required 分数无效、`IsRequiredSatisfied == false` 或 required `BuildIntent` 失败，都会让当前 Terminal 候选失败。allowed Support 构建失败时只丢弃该 Support。

一个 Support 如果占用多个单占用 channel，必须赢得全部这些 channel。多个 required Support 争同一个单占用 channel 时，index 小的先占，后续 required 无法满足并导致当前 Terminal 候选失败。

## 提交与执行

`Execute` / `Emergency` 模式会先调用 Terminal `Execute`，只有 Terminal 成功后才执行 Support。`Prepare` / `Committed` 模式不会调用 Terminal `Execute`，只执行 Support。

Terminal 成功且 `LastScore.LockTicks > 0` 时进入 committed 状态。后续 `Evaluate` 会调用当前 Terminal 的 `IsCommitFinished` 判断是否结束；未结束时通过 `CanBeInterrupted` 判断是否允许紧急候选打断。Emergency 在 `Evaluate` 阶段只记录意图，真正清除旧 committed 发生在 `Execute` 阶段；Emergency 执行失败或拒绝后不会恢复旧 committed。

Support 执行失败或拒绝不会阻断其它 Support，整体结果返回 `PartialFailed`。
