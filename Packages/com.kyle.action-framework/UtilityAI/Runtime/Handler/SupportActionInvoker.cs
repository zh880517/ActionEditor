using System;

namespace UtilityAI
{
    internal sealed class SupportActionInvoker<TContext, TConfig> : ISupportActionInvoker<TContext>
        where TConfig : SupportActionConfig
    {
        private readonly SupportActionHandler<TContext, TConfig> _handler;

        public SupportActionInvoker(SupportActionHandler<TContext, TConfig> handler)
        {
            _handler = handler;
        }

        public Type ConfigType { get { return typeof(TConfig); } }

        public SupportScore Score(TContext context, SupportActionConfig config, SupportScoreInput input)
        {
            return _handler.Score(context, (TConfig)config, input);
        }

        public UtilityIntentBuildStatus BuildIntent(TContext context, SupportActionConfig config, SupportIntentBuildInput input)
        {
            return _handler.BuildIntent(context, (TConfig)config, input);
        }

        public UtilityActionExecutionStatus Execute(TContext context, SupportActionConfig config, SupportExecuteInput input)
        {
            return _handler.Execute(context, (TConfig)config, input);
        }
    }
}
