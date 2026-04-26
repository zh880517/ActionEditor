namespace GOAP
{
    // 行动执行器接口（非泛型），对应 FlowGraph 的 IFlowNodeExecutor
    // 无状态：所有执行状态由 Agent 或行动数据本身持有
    // 子类通过 TActionExecutor<T> 实现，并注册到 ActionExecutor<T>.Executor
    public interface IActionExecutor
    {
        // 运行时可行性检查（规划期 + 执行前）
        bool IsAchievable(WorldState current, IActionData data);

        // 行动首次开始执行时调用一次（初始化内部状态、播放动画等）
        void OnEnter(Agent agent, IActionData data);

        // 每帧调用，返回 Running 表示继续，Completed/Failed 触发 OnExit
        ActionStatus OnUpdate(Agent agent, IActionData data);

        // 行动正常结束（Completed 或 Failed）时调用，用于清理
        void OnExit(Agent agent, IActionData data);

        // 行动被外部中断（重规划、目标切换）时调用
        void OnAbort(Agent agent, IActionData data);
    }
}
