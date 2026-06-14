namespace UtilityAI
{
    /// <summary>
    /// Support 支援动作配置基类，用于描述依附 Terminal 的通道占用和最短保持时间。
    /// </summary>
    [System.Serializable]
    public abstract class SupportActionConfig : UtilityActionConfig
    {
        public UtilitySupportChannel[] Channels;
        public int MinHoldTicks;
    }
}
