namespace GOAP
{
    // 行动运行时容器，对应 FlowGraph 的 TFlowNodeRuntimeData<T>
    // 持有纯数据 struct T，并通过 ActionExecutor<T>.Executor 分发生命周期调用
    // 行为逻辑全部在 TActionExecutor<T> 子类中实现，此类为 sealed，不可继承
    //
    // 生命周期：
    //   IsAchievable  → 规划期 + 执行前检查
    //   OnEnter       → 首次 Perform 时调用一次
    //   OnUpdate      → 每帧 Perform 时调用，返回 Running 则继续
    //   OnExit        → Completed / Failed 时自动调用
    //   OnAbort       → Agent 重规划或目标切换时调用
    public sealed class Action<T> : IAction where T : struct, IActionData
    {
        private readonly T _data;
        private bool _entered;

        public Action(T data)
        {
            _data = data;
        }

        // --- IAction 实现 ---

        public string Id => _data.Id;
        public float Cost => _data.Cost;
        public WorldState Preconditions => _data.Preconditions;
        public WorldState Effects => _data.Effects;

        public bool IsAchievable(WorldState current)
        {
            var executor = ActionExecutor<T>.Executor;
            return executor == null || executor.IsAchievable(current, _data);
        }

        // 首次调用触发 OnEnter，之后每帧调用 OnUpdate
        // OnUpdate 返回非 Running 时自动调用 OnExit
        public ActionStatus Perform(Agent agent)
        {
            var executor = ActionExecutor<T>.Executor;
            if (executor == null)
                return ActionStatus.Failed;

            if (!_entered)
            {
                _entered = true;
                executor.OnEnter(agent, _data);
            }

            var status = executor.OnUpdate(agent, _data);

            if (status != ActionStatus.Running)
            {
                executor.OnExit(agent, _data);
                _entered = false;
            }

            return status;
        }

        // 被 Agent 中断时调用（重规划、目标切换）
        public void Abort(Agent agent)
        {
            if (_entered)
            {
                ActionExecutor<T>.Executor?.OnAbort(agent, _data);
                _entered = false;
            }
        }
    }
}

