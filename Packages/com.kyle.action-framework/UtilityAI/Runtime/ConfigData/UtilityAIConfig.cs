namespace UtilityAI
{
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
}
