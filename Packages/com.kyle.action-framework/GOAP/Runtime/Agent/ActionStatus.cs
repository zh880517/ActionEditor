namespace GOAP
{
    // 行动执行状态
    public enum ActionStatus
    {
        // 行动正在执行中，需要继续 Tick
        Running,
        // 行动执行完成
        Completed,
        // 行动执行失败，触发重规划
        Failed
    }

    // Agent 整体状态
    public enum AgentStatus
    {
        // 没有可用目标，处于空闲
        Idle,
        // 正在规划（本帧内同步完成）
        Planning,
        // 正在执行计划中的某个行动
        Executing,
        // 规划失败（无解），等待下次触发
        Failed
    }
}
