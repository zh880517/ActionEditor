using System.Collections.Generic;

namespace GOAP
{
    // 规划结果：一组有序的行动序列
    public class Plan
    {
        private readonly List<IAction> _actions;
        private readonly bool _isValid;

        public Plan(List<IAction> actions, float totalCost)
            : this(actions, totalCost, actions != null && actions.Count > 0)
        {
        }

        private Plan(List<IAction> actions, float totalCost, bool isValid)
        {
            _actions = actions ?? new List<IAction>();
            TotalCost = totalCost;
            _isValid = isValid;
        }

        // 规划出的行动序列（按执行顺序排列）
        public IReadOnlyList<IAction> Actions => _actions;

        // 计划总代价
        public float TotalCost { get; }

        // 计划是否有效（有至少一个行动）
        public bool IsValid => _isValid;

        // 空计划（规划失败时使用）
        public static Plan Empty { get; } = new Plan(new List<IAction>(), 0f, true);

        public static Plan Invalid { get; } = new Plan(new List<IAction>(), 0f, false);
    }
}
