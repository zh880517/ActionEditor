namespace UtilityAI
{
    /// <summary>
    /// UtilityAI 动作配置基类，承载通用标识和显示类型名。
    /// </summary>
    [System.Serializable]
    public abstract class UtilityActionConfig
    {
        public string ActionId;
        public abstract string ActionType { get; }
    }

    /// <summary>
    /// Terminal 主动作配置基类，每次决策最多选择一个 Terminal。
    /// </summary>
    [System.Serializable]
    public abstract class TerminalActionConfig : UtilityActionConfig
    {
    }

    /// <summary>
    /// Support 支援动作配置基类，用于描述依附 Terminal 的通道占用和最短保持时间。
    /// </summary>
    [System.Serializable]
    public abstract class SupportActionConfig : UtilityActionConfig
    {
        public UtilitySupportChannel[] Channels;
        public int MinHoldTicks;
    }

    /// <summary>
    /// UtilityAI 运行时配置，包含 Terminal、Support 和全局仲裁参数。
    /// </summary>
    public sealed class UtilityAIConfig
    {
        public TerminalActionConfig[] TerminalActions;
        public SupportActionConfig[] SupportActions;
        public int MaxSupportCount;
        public float RepositionMargin;
        public float IntentStickiness;
        public int PreparationTimeoutTicks;
    }

    /// <summary>
    /// 由业务上下文实现，用于在每轮意图构建前清理强类型意图状态。
    /// </summary>
    public interface IUtilityIntentState
    {
        void ResetUtilityIntentState();
    }

    /// <summary>
    /// 由业务上下文实现，用于向 Runtime 暴露 Support 的 required、allowed 和 forbidden 约束。
    /// </summary>
    public interface IUtilitySupportConstraintProvider
    {
        bool IsSupportRequired(SupportActionConfig config, int supportIndex);
        bool IsSupportAllowed(SupportActionConfig config, int supportIndex);
        bool IsSupportForbidden(SupportActionConfig config, int supportIndex);
    }

    /// <summary>
    /// 一次 Execute 调用的整体执行结果。
    /// </summary>
    public enum UtilityExecutionStatus
    {
        Success,
        Failed,
        Rejected,
        PartialFailed
    }

    /// <summary>
    /// Runtime 对一次 Execute 调用返回的结果数据。
    /// </summary>
    public struct UtilityExecutionResult
    {
        public UtilityExecutionStatus Status;
    }

    /// <summary>
    /// 当前决策的意图模式，决定 Execute 阶段调用 Terminal 或 Support 的方式。
    /// </summary>
    public enum UtilityIntentMode
    {
        None,
        Execute,
        Prepare,
        Committed,
        Emergency
    }

    /// <summary>
    /// Evaluate 产出的轻量决策结果，不暴露内部 Support 选择数组所有权。
    /// </summary>
    public struct UtilityDecisionResult
    {
        public int DecisionVersion;
        public UtilityIntentMode Mode;
        public int SelectedTerminalIndex;
        public int SelectedSupportCount;
    }

    /// <summary>
    /// 单个 Terminal 或 Support 动作在执行阶段返回的状态。
    /// </summary>
    public enum UtilityActionExecutionStatus
    {
        Success,
        Failed,
        Rejected
    }

    /// <summary>
    /// Terminal 或 Support 在意图构建阶段返回的状态。
    /// </summary>
    public enum UtilityIntentBuildStatus
    {
        Success,
        Failed
    }

    /// <summary>
    /// Support 占用的默认仲裁通道。
    /// </summary>
    public enum UtilitySupportChannel
    {
        Movement,
        Facing,
        Positioning,
        Validation,
        Modifier
    }

    /// <summary>
    /// 业务层传入 Runtime 的时间信息，Runtime 不直接读取 Unity 时间。
    /// </summary>
    public struct UtilityTickInfo
    {
        public float Time;
        public float DeltaTime;
        public int TickIndex;
    }

    /// <summary>
    /// Terminal 评分结果，描述执行、准备、紧急和提交锁定能力。
    /// </summary>
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

    /// <summary>
    /// Support 评分结果，描述是否可参与仲裁以及 required 约束是否满足。
    /// </summary>
    public struct SupportScore
    {
        public bool IsValid;
        public float Score;
        public bool IsRequiredSatisfied;
    }

    /// <summary>
    /// 传入 Terminal Score 的上下文数据。
    /// </summary>
    public struct TerminalScoreInput
    {
        public UtilityTickInfo TickInfo;
        public int TerminalIndex;
        public int DecisionVersion;
        public bool HasCommittedTerminal;
        public int CommittedTerminalIndex;
    }

    /// <summary>
    /// 传入 Terminal BuildIntent 的上下文数据。
    /// </summary>
    public struct TerminalIntentBuildInput
    {
        public UtilityTickInfo TickInfo;
        public int TerminalIndex;
        public int DecisionVersion;
        public UtilityIntentMode Mode;
        public TerminalScore Score;
    }

    /// <summary>
    /// 传入 Terminal Execute 的上下文数据。
    /// </summary>
    public struct TerminalExecuteInput
    {
        public UtilityTickInfo TickInfo;
        public int TerminalIndex;
        public int DecisionVersion;
        public UtilityIntentMode Mode;
        public TerminalScore Score;
    }

    /// <summary>
    /// 传入当前 committed Terminal 打断判断的上下文数据。
    /// </summary>
    public struct TerminalInterruptInput
    {
        public UtilityTickInfo TickInfo;
        public int TerminalIndex;
        public int DecisionVersion;
        public int CommitStartTick;
        public int CommitUntilTick;
        public TerminalScore Score;
    }

    /// <summary>
    /// 传入当前 committed Terminal 完成判断的上下文数据。
    /// </summary>
    public struct TerminalCommitInput
    {
        public UtilityTickInfo TickInfo;
        public int TerminalIndex;
        public int DecisionVersion;
        public int CommitStartTick;
        public int CommitUntilTick;
        public TerminalScore Score;
    }

    /// <summary>
    /// 传入 Support Score 的上下文数据。
    /// </summary>
    public struct SupportScoreInput
    {
        public UtilityTickInfo TickInfo;
        public int SupportIndex;
        public int SelectedTerminalIndex;
        public int DecisionVersion;
        public UtilityIntentMode Mode;
        public bool IsRequired;
        public bool IsAllowed;
    }

    /// <summary>
    /// 传入 Support BuildIntent 的上下文数据。
    /// </summary>
    public struct SupportIntentBuildInput
    {
        public UtilityTickInfo TickInfo;
        public int SupportIndex;
        public int SelectedTerminalIndex;
        public int DecisionVersion;
        public UtilityIntentMode Mode;
        public bool IsRequired;
        public SupportScore Score;
    }

    /// <summary>
    /// 传入 Support Execute 的上下文数据。
    /// </summary>
    public struct SupportExecuteInput
    {
        public UtilityTickInfo TickInfo;
        public int SupportIndex;
        public int SelectedTerminalIndex;
        public int DecisionVersion;
        public UtilityIntentMode Mode;
        public bool IsRequired;
        public SupportScore Score;
    }

    /// <summary>
    /// Runtime 缓存后的 Terminal 动作数据，避免 Tick 中重新查询注册表。
    /// </summary>
    public struct TerminalActionRuntime<TContext>
    {
        public TerminalActionConfig Config;
        public ITerminalActionInvoker<TContext> Handler;
        public TerminalScore LastScore;
        public int LastSelectedTick;
    }

    /// <summary>
    /// Runtime 缓存后的 Support 动作数据，记录评分、激活状态和 MinHold 记忆。
    /// </summary>
    public struct SupportActionRuntime<TContext>
    {
        public SupportActionConfig Config;
        public ISupportActionInvoker<TContext> Handler;
        public SupportScore LastScore;
        public bool IsActive;
        public int MinHoldUntilTick;
    }
}
