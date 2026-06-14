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
