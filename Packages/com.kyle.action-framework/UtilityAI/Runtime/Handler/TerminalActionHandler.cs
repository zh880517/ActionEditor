using System;

namespace UtilityAI
{
    /// <summary>
    /// Terminal 主动作处理器基类，负责评分、意图构建、执行和提交锁定判断。
    /// </summary>
    public abstract class TerminalActionHandler<TContext, TConfig>
        where TConfig : TerminalActionConfig
    {
        public abstract TerminalScore Score(TContext context, TConfig config, TerminalScoreInput input);
        public abstract UtilityIntentBuildStatus BuildIntent(TContext context, TConfig config, TerminalIntentBuildInput input);
        public abstract UtilityActionExecutionStatus Execute(TContext context, TConfig config, TerminalExecuteInput input);
        public abstract bool CanBeInterrupted(TContext context, TConfig config, TerminalInterruptInput input);
        public abstract bool IsCommitFinished(TContext context, TConfig config, TerminalCommitInput input);
    }
}
