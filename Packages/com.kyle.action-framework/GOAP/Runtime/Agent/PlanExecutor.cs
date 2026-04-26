namespace GOAP
{
    // 计划执行器：管理 IAction 序列的完整生命周期，从 Agent.Tick 中抽离
    // 保证：
    //   OnEnter  — 每个 Action 仅调用一次
    //   OnExit   — Completed 和 Failed 时均调用
    //   OnAbort  — Abort() 时调用（仅在 Action 已 Enter 的情况下）
    internal enum PlanExecutorStatus { Idle, Executing, Done, Failed }

    internal class PlanExecutor
    {
        private Plan _plan;
        private int _index;
        private bool _entered;

        public PlanExecutorStatus Status { get; private set; } = PlanExecutorStatus.Idle;

        // 当前正在执行的 Action（供外部只读展示）
        public IAction CurrentAction =>
            _plan != null && _index < _plan.Actions.Count ? _plan.Actions[_index] : null;

        // 接收新计划并重置执行状态
        // 注意：调用方应在 SetPlan 前先调用 Abort() 中断当前 Action
        public void SetPlan(Plan plan)
        {
            _plan = plan;
            _index = 0;
            _entered = false;
            Status = (plan != null && plan.IsValid)
                ? PlanExecutorStatus.Executing
                : PlanExecutorStatus.Idle;
        }

        // 每帧驱动，返回当前执行状态
        public PlanExecutorStatus Tick(AgentContext ctx, float deltaTime)
        {
            if (Status != PlanExecutorStatus.Executing) return Status;

            if (_plan == null || _index >= _plan.Actions.Count)
            {
                Status = PlanExecutorStatus.Done;
                return Status;
            }

            var action = _plan.Actions[_index];

            // 执行前运行时检查：前置条件或 IsApplicable 不满足时触发重规划
            if (!ctx.WorldState.Satisfies(action.Preconditions) || !action.IsApplicable(ctx.WorldState))
            {
                Status = PlanExecutorStatus.Failed;
                return Status;
            }

            // Enter（每个 Action 只调用一次）
            if (!_entered)
            {
                _entered = true;
                action.OnEnter(ctx);
            }

            var result = action.OnUpdate(ctx, deltaTime);

            switch (result)
            {
                case ActionStatus.Completed:
                    action.OnExit(ctx);
                    ctx.WorldState.Apply(action.Effects);
                    _entered = false;
                    _index++;
                    if (_index >= _plan.Actions.Count)
                        Status = PlanExecutorStatus.Done;
                    break;

                case ActionStatus.Failed:
                    action.OnExit(ctx);
                    _entered = false;
                    Status = PlanExecutorStatus.Failed;
                    break;

                // ActionStatus.Running：继续等待
            }

            return Status;
        }

        // 中断当前 Action（重规划或目标切换时调用）
        public void Abort(AgentContext ctx)
        {
            if (Status == PlanExecutorStatus.Executing && _entered && CurrentAction != null)
            {
                CurrentAction.OnAbort(ctx);
                _entered = false;
            }
            _plan = null;
            _index = 0;
            Status = PlanExecutorStatus.Idle;
        }
    }
}
