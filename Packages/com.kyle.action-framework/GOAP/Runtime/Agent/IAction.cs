namespace GOAP
{
    // 行动接口：规划数据 + 执行生命周期
    // 游戏代码通过 RuntimeAction<T> + TActionRunner<T> 实现，注册到 Agent.Actions
    public interface IAction
    {
        // 行动唯一标识，与编辑器中的 ActionId 对应
        string Id { get; }

        // 规划代价：f(n) 计算中的边权重，越低越优先
        float Cost { get; }

        // 前置条件：规划与执行前检查（WorldState subset）
        WorldState Preconditions { get; }

        // 效果：行动完成后应用到 WorldState 的变化
        WorldState Effects { get; }

        // 规划时与执行前的可行性检查（纯函数，无副作用）
        // 用于动态条件过滤，如距离、冷却、资源等
        bool IsApplicable(WorldState current);

        // 行动开始时调用一次（初始化动画、变量等）
        void OnEnter(AgentContext ctx);

        // 每帧调用，deltaTime 为帧时间；返回 Running 表示继续
        ActionStatus OnUpdate(AgentContext ctx, float deltaTime);

        // 行动正常结束后调用（Completed 或 Failed 均触发）
        void OnExit(AgentContext ctx);

        // 行动被外部中断时调用（重规划、目标切换）
        void OnAbort(AgentContext ctx);
    }
}
