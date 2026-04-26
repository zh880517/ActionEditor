namespace GOAP
{
    // 行动数据接口，用于标记行动的纯数据 struct，对应 FlowGraph 的节点 struct T
    // 实现此接口的 struct 只存储数据，不包含任何行为逻辑
    // 行为逻辑通过 TActionExecutor<T> 实现，并注册到 ActionExecutor<T>.Executor
    public interface IActionData
    {
        string Id { get; }
        float Cost { get; }
        WorldState Preconditions { get; }
        WorldState Effects { get; }
    }
}
