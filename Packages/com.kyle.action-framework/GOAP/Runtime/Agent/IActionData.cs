namespace GOAP
{
    // 行动数据接口，用于标记行动的数据 struct
    // 与 FlowGraph 的 struct T 模式对应：
    //   struct MyActionData : IActionData { ... }
    //   class MyAction : Action<MyActionData> { ... }
    public interface IActionData
    {
        // 行动唯一标识，与编辑器 ActionId 对应
        string Id { get; }

        // 规划代价
        float Cost { get; }

        // 前置条件
        WorldState Preconditions { get; }

        // 效果
        WorldState Effects { get; }
    }
}
