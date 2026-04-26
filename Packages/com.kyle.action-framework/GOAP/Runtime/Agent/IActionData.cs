namespace GOAP
{
    // 行动执行数据接口，struct 只存储类型专有字段（如攻击范围、技能 ID 等）
    // 规划数据（Cost / Preconditions / Effects）统一由 SerializedActionData 持有
    // 行为逻辑通过 TActionRunner<T> 实现，并注册到 ActionRunner<T>.Runner
    public interface IActionData
    {
        // 行动唯一标识，与编辑器中的 ActionId 对应
        string Id { get; }
    }
}
