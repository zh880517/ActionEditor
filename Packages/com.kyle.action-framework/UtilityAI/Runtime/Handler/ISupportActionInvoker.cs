using System;

namespace UtilityAI
{
    /// <summary>
    /// Support 处理器的非泛型调用接口，用于 Runtime 缓存并按配置实例分发到强类型 Handler。
    /// </summary>
    public interface ISupportActionInvoker<TContext>
    {
        Type ConfigType { get; }
        SupportScore Score(TContext context, SupportActionConfig config, SupportScoreInput input);
        UtilityIntentBuildStatus BuildIntent(TContext context, SupportActionConfig config, SupportIntentBuildInput input);
        UtilityActionExecutionStatus Execute(TContext context, SupportActionConfig config, SupportExecuteInput input);
    }
}
