using System.Collections.Generic;

namespace GOAP
{
    // NPC AI 驱动器，每帧通过 Tick() 推进目标选择 → 规划 → 执行流程
    // 游戏代码负责：
    //   1. 每帧更新 WorldState（感知层写入）
    //   2. 注册 Goals 和 AvailableActions
    //   3. 调用 Tick(deltaTime)
    public class Agent
    {
        // 当前世界状态，由外部感知层负责更新
        public WorldState WorldState { get; } = new WorldState();

        // 注册的目标列表
        public List<IGoal> Goals { get; } = new List<IGoal>();

        // 可用行动列表
        public List<IAction> AvailableActions { get; } = new List<IAction>();

        // 规划最大深度
        public int MaxPlanDepth { get; set; } = 10;

        public IGoal CurrentGoal { get; private set; }
        public Plan CurrentPlan { get; private set; }
        public IAction CurrentAction { get; private set; }
        public AgentStatus Status { get; private set; } = AgentStatus.Idle;

        // 重规划请求标志
        private bool _replanRequested;

        // 当前计划中正在执行的行动索引
        private int _currentActionIndex;

        // 当前目标发生变化时调用，子类可重写以响应目标切换（如切换动画、重置状态）
        protected virtual void OnGoalChanged(IGoal newGoal) { }

        // 重规划完成时调用，子类可重写（plan 为 null 表示规划失败）
        protected virtual void OnPlanChanged(Plan newPlan) { }

        // 主驱动入口，每帧调用
        public void Tick(float deltaTime)
        {
            // 选出当前最优目标
            var bestGoal = SelectGoal();

            // 目标切换或请求重规划 → 中断当前行动，重新规划
            bool goalChanged = bestGoal != CurrentGoal;
            if (goalChanged || _replanRequested)
            {
                AbortCurrentAction();
                CurrentGoal = bestGoal;
                _replanRequested = false;

                if (bestGoal == null)
                {
                    Status = AgentStatus.Idle;
                    CurrentPlan = null;
                    CurrentAction = null;
                    return;
                }

                // 规划
                Status = AgentStatus.Planning;
                var plan = RunPlanner(bestGoal);

                CurrentPlan = plan;
                _currentActionIndex = 0;
                OnPlanChanged(plan);

                if (goalChanged)
                    OnGoalChanged(bestGoal);

                if (plan == null || !plan.IsValid)
                {
                    Status = AgentStatus.Failed;
                    CurrentAction = null;
                    return;
                }
            }

            if (CurrentPlan == null || !CurrentPlan.IsValid)
            {
                Status = AgentStatus.Idle;
                return;
            }

            // 执行当前行动
            Status = AgentStatus.Executing;
            ExecuteCurrentAction();
        }

        // 请求在下一帧重新规划（外部事件驱动，如受伤、发现新敌人等）
        public void RequestReplan()
        {
            _replanRequested = true;
        }

        // 从 Goals 中选出优先级最高且有效的目标
        private IGoal SelectGoal()
        {
            IGoal best = null;
            float bestPriority = float.MinValue;
            foreach (var goal in Goals)
            {
                if (!goal.IsValid(WorldState))
                    continue;
                float priority = goal.GetPriority(WorldState);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    best = goal;
                }
            }
            return best;
        }

        // 调用规划器
        private Plan RunPlanner(IGoal goal)
        {
            return Planner.Plan(
                WorldState,
                goal.GetDesiredState(),
                AvailableActions,
                MaxPlanDepth);
        }

        // 执行计划中当前行动，处理完成/失败/推进
        private void ExecuteCurrentAction()
        {
            if (_currentActionIndex >= CurrentPlan.Actions.Count)
            {
                // 计划全部执行完毕
                CurrentAction = null;
                CurrentPlan = null;
                Status = AgentStatus.Idle;
                return;
            }

            CurrentAction = CurrentPlan.Actions[_currentActionIndex];

            // 运行时再次检查前置条件
            if (!WorldState.Satisfies(CurrentAction.Preconditions) || !CurrentAction.IsAchievable(WorldState))
            {
                // 前置条件不再满足，触发重规划
                RequestReplan();
                return;
            }

            var status = CurrentAction.Perform(this);
            switch (status)
            {
                case ActionStatus.Completed:
                    // 将行动效果应用到世界状态，推进到下一个行动
                    WorldState.Apply(CurrentAction.Effects);
                    _currentActionIndex++;
                    CurrentAction = null;
                    break;

                case ActionStatus.Failed:
                    // 行动失败，触发重规划
                    CurrentAction = null;
                    RequestReplan();
                    break;

                case ActionStatus.Running:
                    // 继续执行，下帧再 Tick
                    break;
            }
        }

        // 中断当前正在执行的行动
        private void AbortCurrentAction()
        {
            if (CurrentAction != null)
            {
                CurrentAction.Abort(this);
                CurrentAction = null;
            }
            _currentActionIndex = 0;
        }
    }
}
