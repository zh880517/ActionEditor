using System;

namespace UtilityAI
{
    /// <summary>
    /// Terminal 处理器的非泛型调用接口，用于 Runtime 缓存并按配置实例分发到强类型 Handler。
    /// </summary>
    public interface ITerminalActionInvoker<TContext>
    {
        Type ConfigType { get; }
        TerminalScore Score(TContext context, TerminalActionConfig config, TerminalScoreInput input);
        UtilityIntentBuildStatus BuildIntent(TContext context, TerminalActionConfig config, TerminalIntentBuildInput input);
        UtilityActionExecutionStatus Execute(TContext context, TerminalActionConfig config, TerminalExecuteInput input);
        bool CanBeInterrupted(TContext context, TerminalActionConfig config, TerminalInterruptInput input);
        bool IsCommitFinished(TContext context, TerminalActionConfig config, TerminalCommitInput input);
    }
}
