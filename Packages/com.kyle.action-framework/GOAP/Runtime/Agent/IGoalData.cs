namespace GOAP
{
    // 目标数据接口，用于标记目标的数据 struct
    // 与 IActionData 对称：
    //   struct SurviveData : IGoalData { ... }
    //   class SurviveGoal : Goal<SurviveData> { ... }
    public interface IGoalData
    {
        // 目标唯一标识，与编辑器 GoalId 对应
        string Id { get; }

        // 基础优先级（无动态逻辑时直接使用）
        float BasePriority { get; }

        // 期望终态
        WorldState DesiredState { get; }
    }
}
