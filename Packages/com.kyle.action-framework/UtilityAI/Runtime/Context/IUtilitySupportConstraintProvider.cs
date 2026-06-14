namespace UtilityAI
{
    /// <summary>
    /// 由业务上下文实现，用于向 Runtime 暴露 Support 的 required、allowed 和 forbidden 约束。
    /// </summary>
    public interface IUtilitySupportConstraintProvider
    {
        bool IsSupportRequired(SupportActionConfig config, int supportIndex);
        bool IsSupportAllowed(SupportActionConfig config, int supportIndex);
        bool IsSupportForbidden(SupportActionConfig config, int supportIndex);
    }
}
