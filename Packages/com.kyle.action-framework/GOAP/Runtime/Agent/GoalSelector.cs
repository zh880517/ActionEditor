using System.Collections.Generic;

namespace GOAP
{
    // 目标选择器，封装滞后切换逻辑，从 Agent.Tick 中抽离
    // 滞后规则：新目标的优先级优势必须严格大于当前目标的 InsistenceBias 才会切换
    // 防止因优先级微小波动导致每帧频繁切换目标
    internal class GoalSelector
    {
        private IGoal _currentGoal;

        public IGoal CurrentGoal => _currentGoal;

        // 从 goals 中选出最优目标并应用滞后逻辑，返回本帧应执行的目标（可为 null）
        public IGoal Select(WorldState current, IReadOnlyList<IGoal> goals)
        {
            // 找出优先级最高的有效目标
            IGoal best = null;
            float bestPriority = float.MinValue;
            foreach (var goal in goals)
            {
                if (!goal.IsValid(current)) continue;
                float priority = goal.GetPriority(current);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    best = goal;
                }
            }

            if (best == null)
            {
                _currentGoal = null;
                return null;
            }

            // 滞后检查：当前目标仍有效时，新目标的优先级优势必须 > InsistenceBias 才切换
            if (_currentGoal != null && _currentGoal != best && _currentGoal.IsValid(current))
            {
                float currentPriority = _currentGoal.GetPriority(current);
                float advantage = bestPriority - currentPriority;
                if (advantage <= _currentGoal.InsistenceBias)
                    return _currentGoal;
            }

            _currentGoal = best;
            return best;
        }

        // 强制清除当前目标（Agent 重置时调用）
        public void Reset()
        {
            _currentGoal = null;
        }
    }
}
