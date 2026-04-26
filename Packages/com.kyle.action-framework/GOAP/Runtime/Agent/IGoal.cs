namespace GOAP
{
    // 目标接口：描述 NPC 希望达到的终态
    // 游戏代码实现此接口，注册到 Agent.Goals
    public interface IGoal
    {
        // 目标唯一标识，与编辑器中的 GoalId 对应
        string Id { get; }

        // 动态优先级：根据当前世界状态返回优先级，越大越优先
        float GetPriority(WorldState current);

        // 目标终态：Planner 搜索时的目标 WorldState（subset）
        WorldState GetDesiredState();

        // 当前是否有效（不满足条件的目标不会被选中）
        bool IsValid(WorldState current);

        // 目标滘后切换閘值：新目标的优先级优势必须 > InsistenceBias 才会切换
        // 局油目标频繁切换，默认为 0（无滘后效果）
        float InsistenceBias { get; }
    }
}
