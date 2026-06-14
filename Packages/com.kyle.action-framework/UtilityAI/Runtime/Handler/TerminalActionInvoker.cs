using System;

namespace UtilityAI
{
    internal sealed class TerminalActionInvoker<TContext, TConfig> : ITerminalActionInvoker<TContext>
        where TConfig : TerminalActionConfig
    {
        private readonly TerminalActionHandler<TContext, TConfig> _handler;

        public TerminalActionInvoker(TerminalActionHandler<TContext, TConfig> handler)
        {
            _handler = handler;
        }

        public Type ConfigType { get { return typeof(TConfig); } }

        public TerminalScore Score(TContext context, TerminalActionConfig config, TerminalScoreInput input)
        {
            return _handler.Score(context, (TConfig)config, input);
        }

        public UtilityIntentBuildStatus BuildIntent(TContext context, TerminalActionConfig config, TerminalIntentBuildInput input)
        {
            return _handler.BuildIntent(context, (TConfig)config, input);
        }

        public UtilityActionExecutionStatus Execute(TContext context, TerminalActionConfig config, TerminalExecuteInput input)
        {
            return _handler.Execute(context, (TConfig)config, input);
        }

        public bool CanBeInterrupted(TContext context, TerminalActionConfig config, TerminalInterruptInput input)
        {
            return _handler.CanBeInterrupted(context, (TConfig)config, input);
        }

        public bool IsCommitFinished(TContext context, TerminalActionConfig config, TerminalCommitInput input)
        {
            return _handler.IsCommitFinished(context, (TConfig)config, input);
        }
    }
}
