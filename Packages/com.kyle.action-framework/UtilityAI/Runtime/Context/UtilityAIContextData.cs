namespace UtilityAI
{
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
