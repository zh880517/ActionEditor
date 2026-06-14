using System;

namespace UtilityAI
{
    /// <summary>
    /// Support 支援动作处理器基类，负责评分、意图构建和执行。
    /// </summary>
    public abstract class SupportActionHandler<TContext, TConfig>
        where TConfig : SupportActionConfig
    {
        public abstract SupportScore Score(TContext context, TConfig config, SupportScoreInput input);
        public abstract UtilityIntentBuildStatus BuildIntent(TContext context, TConfig config, SupportIntentBuildInput input);
        public abstract UtilityActionExecutionStatus Execute(TContext context, TConfig config, SupportExecuteInput input);
    }
}
