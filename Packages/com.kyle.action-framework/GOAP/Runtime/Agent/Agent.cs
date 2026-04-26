using System.Collections.Generic;

namespace GOAP
{
    // NPC AI 驱动器（薄编排层）
    // 职责拆分：
    //   目标选择  → GoalSelector（含滞后切换逻辑）
    //   计划执行  → PlanExecutor（完整 Action 生命周期管理）
    //   规划搜索  → Planner（静态，反向 A*）
    // 游戏代码负责：
    //   1. 每帧通过感知层更新 WorldState
    //   2. 注册 Goals 和 Actions
    //   3. 调用 Tick(deltaTime)
    public class Agent
    {
        // 当前世界状态，由外部感知层每帧写入
        public WorldState WorldState { get; } = new WorldState();

        // 注册的目标列表
        public List<IGoal> Goals { get; } = new List<IGoal>();

        // 可用行动列表（规划与执行共用）
        public List<IAction> Actions { get; } = new List<IAction>();

        // 规划最大深度（反向 A* 搜索层数上限）
        public int MaxPlanDepth { get; set; } = 10;

        public IGoal CurrentGoal { get; private set; }
        public Plan CurrentPlan { get; private set; }

        // 当前正在执行的 Action（由 PlanExecutor 持有，只读展示）
        public IAction CurrentAction => _planExecutor.CurrentAction;

        public AgentStatus Status { get; private set; } = AgentStatus.Idle;

        // --- 内部组件 ---
        private readonly GoalSelector _goalSelector = new GoalSelector();
        private readonly PlanExecutor _planExecutor = new PlanExecutor();
        private readonly AgentContext _context;
        private bool _replanRequested;

        public Agent()
        {
            _context = new AgentContext(this);
        }

        // 主驱动入口，每帧调用
        public void Tick(float deltaTime)
        {
            // 1. 目标选择（含滞后逻辑）
            var bestGoal = _goalSelector.Select(WorldState, Goals);

            // 2. 目标切换或重规划请求 → 中断当前 Action，重新规划
            bool goalChanged = bestGoal != CurrentGoal;
            if (goalChanged || _replanRequested)
            {
                _planExecutor.Abort(_context);
                CurrentGoal = bestGoal;
                _replanRequested = false;

                if (bestGoal == null)
                {
                    Status = AgentStatus.Idle;
                    CurrentPlan = null;
                    return;
                }

                // 3. 规划（同步，本帧内完成）
                Status = AgentStatus.Planning;
                CurrentPlan = Planner.Plan(WorldState, bestGoal.GetDesiredState(), Actions, MaxPlanDepth);
                _planExecutor.SetPlan(CurrentPlan);

                if (goalChanged) OnGoalChanged(bestGoal);
                OnPlanChanged(CurrentPlan);

                if (!CurrentPlan.IsValid)
                {
                    Status = AgentStatus.Failed;
                    return;
                }
            }

            if (CurrentPlan == null || !CurrentPlan.IsValid)
            {
                Status = AgentStatus.Idle;
                return;
            }

            // 4. 执行计划
            Status = AgentStatus.Executing;
            var execStatus = _planExecutor.Tick(_context, deltaTime);

            switch (execStatus)
            {
                case PlanExecutorStatus.Done:
                    // 所有 Action 执行完毕，目标达成
                    CurrentPlan = null;
                    Status = AgentStatus.Idle;
                    break;

                case PlanExecutorStatus.Failed:
                    // 执行失败，下帧触发重规划
                    RequestReplan();
                    break;
            }
        }

        // 请求在下一帧重新规划（外部事件驱动：受伤、发现敌人、资源消耗等）
        public void RequestReplan()
        {
            _replanRequested = true;
        }

        // 目标发生变化时调用，子类可重写（如切换动画状态机）
        protected virtual void OnGoalChanged(IGoal newGoal) { }

        // 规划完成时调用，子类可重写（plan.IsValid 为 false 表示规划失败）
        protected virtual void OnPlanChanged(Plan newPlan) { }
    }
}
