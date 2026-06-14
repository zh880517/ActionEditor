using System;

namespace UtilityAI
{
    /// <summary>
    /// UtilityAI Runtime 创建入口，负责校验配置并缓存 Handler。
    /// </summary>
    public static class UtilityAIRuntime
    {
        public static bool TryCreate<TContext>(
            UtilityAIConfig config,
            UtilityActionRegistry<TContext> registry,
            out UtilityAIRuntime<TContext> runtime)
            where TContext : IUtilityIntentState, IUtilitySupportConstraintProvider
        {
            runtime = null;
            if (config == null || registry == null)
                return false;

            if (config.TerminalActions == null || config.TerminalActions.Length == 0)
                return false;

            if (config.MaxSupportCount < 0 ||
                config.RepositionMargin < 0 ||
                config.IntentStickiness < 0 ||
                config.PreparationTimeoutTicks < 0 ||
                !IsFinite(config.RepositionMargin) ||
                !IsFinite(config.IntentStickiness))
                return false;

            var terminalActions = new TerminalActionRuntime<TContext>[config.TerminalActions.Length];
            for (int i = 0; i < config.TerminalActions.Length; i++)
            {
                var terminalConfig = config.TerminalActions[i];
                if (terminalConfig == null)
                    return false;

                ITerminalActionInvoker<TContext> handler;
                if (!registry.TryGetTerminalHandler(terminalConfig.GetType(), out handler))
                    return false;

                terminalActions[i] = new TerminalActionRuntime<TContext>
                {
                    Config = terminalConfig,
                    Handler = handler,
                    LastSelectedTick = -1
                };
            }

            var supportConfigs = config.SupportActions ?? Array.Empty<SupportActionConfig>();
            var supportActions = new SupportActionRuntime<TContext>[supportConfigs.Length];
            for (int i = 0; i < supportConfigs.Length; i++)
            {
                var supportConfig = supportConfigs[i];
                if (supportConfig == null || supportConfig.MinHoldTicks < 0)
                    return false;

                if (!ValidateChannels(supportConfig.Channels))
                    return false;

                ISupportActionInvoker<TContext> handler;
                if (!registry.TryGetSupportHandler(supportConfig.GetType(), out handler))
                    return false;

                supportActions[i] = new SupportActionRuntime<TContext>
                {
                    Config = supportConfig,
                    Handler = handler
                };
            }

            runtime = new UtilityAIRuntime<TContext>(config, terminalActions, supportActions);
            return true;
        }

        private static bool ValidateChannels(UtilitySupportChannel[] channels)
        {
            if (channels == null || channels.Length == 0)
                return false;

            for (int i = 0; i < channels.Length; i++)
            {
                if (!IsValidChannel(channels[i]))
                    return false;

                for (int j = i + 1; j < channels.Length; j++)
                {
                    if (channels[i] == channels[j])
                        return false;
                }
            }

            return true;
        }

        private static bool IsValidChannel(UtilitySupportChannel channel)
        {
            switch (channel)
            {
                case UtilitySupportChannel.Movement:
                case UtilitySupportChannel.Facing:
                case UtilitySupportChannel.Positioning:
                case UtilitySupportChannel.Validation:
                case UtilitySupportChannel.Modifier:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
