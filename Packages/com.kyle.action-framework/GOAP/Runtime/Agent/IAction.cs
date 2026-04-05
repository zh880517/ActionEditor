namespace GOAP
{
    // 行动接口：描述 NPC 可以执行的一个行为
    // 游戏代码实现此接口，注册到 Agent.AvailableActions
    public interface IAction
    {
        // 行动唯一标识，与编辑器中的 ActionId 对应
        string Id { get; }

        // 规划代价：A* 搜索中的边权重，越低越优先被选中
        float Cost { get; }

        // 前置条件：执行此行动所需的世界状态（subset）
        WorldState Preconditions { get; }

        // 效果：执行此行动后对世界状态的改变
        WorldState Effects { get; }

        // 运行时额外可行性检查（如距离、冷却等动态条件）
        // 规划时也会调用此方法过滤不可用的行动
        bool IsAchievable(WorldState current);

        // 执行行动，每帧调用直到返回 Completed 或 Failed
        ActionStatus Perform(Agent agent);

        // 中断行动（重规划或目标切换时调用，用于清理行动内部状态）
        void Abort(Agent agent);
    }
}
