namespace UtilityAI
{
    /// <summary>
    /// 由业务上下文实现，用于在每轮意图构建前清理强类型意图状态。
    /// </summary>
    public interface IUtilityIntentState
    {
        void ResetUtilityIntentState();
    }
}
