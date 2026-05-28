using System.Collections.Generic;

namespace GOAP
{
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

        public IReadOnlyList<IAction> Actions => _actions;

        public float TotalCost { get; }

        public bool IsValid => _isValid;

        public bool HasActions => _actions.Count > 0;

        public int ActionCount => _actions.Count;

        public static Plan Empty { get; } = new Plan(new List<IAction>(), 0f, true);

        public static Plan Invalid { get; } = new Plan(new List<IAction>(), 0f, false);
    }
}
