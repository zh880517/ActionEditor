namespace UtilityAI
{
    /// <summary>
    /// UtilityAI 动作配置基类，承载通用标识和显示类型名。
    /// </summary>
    [System.Serializable]
    public abstract class UtilityActionConfig
    {
        public string ActionId;
        public abstract string ActionType { get; }
    }
}
