using System.Collections.Generic;

namespace GOAP
{
    public struct PlannerOptions
    {
        public int MaxDepth;
        public int MaxExpandedNodes;
        public bool CheckActionApplicability;

        public PlannerOptions(
            int maxDepth,
            int maxExpandedNodes = 512,
            bool checkActionApplicability = false)
        {
            MaxDepth = maxDepth;
            MaxExpandedNodes = maxExpandedNodes;
            CheckActionApplicability = checkActionApplicability;
        }

        public static PlannerOptions Default => new PlannerOptions(10);
    }

    public sealed class Planner
    {
        [System.ThreadStatic]
        private static Planner _shared;

        private readonly Stack<PlannerNode> _nodePool = new Stack<PlannerNode>(64);
        private readonly Stack<WorldState> _statePool = new Stack<WorldState>(64);
        private readonly List<PlannerNode> _allocated = new List<PlannerNode>(64);
        private readonly List<WorldState> _allocatedStates = new List<WorldState>(64);

        private readonly MinHeap<PlannerNode> _openList =
            new MinHeap<PlannerNode>((a, b) => a.F.CompareTo(b.F));

        private readonly HashSet<WorldState> _closedSet =
            new HashSet<WorldState>(WorldStateComparer.Instance);

        private readonly List<IAction> _planBuffer = new List<IAction>();

        public static Plan Plan(
            WorldState currentState,
            WorldState goalState,
            IReadOnlyList<IAction> availableActions,
            int maxDepth = 10)
            => Shared.CreatePlan(currentState, goalState, availableActions, maxDepth);

        public static Plan Plan(
            WorldState currentState,
            WorldState goalState,
            IReadOnlyList<IAction> availableActions,
            PlannerOptions options)
            => Shared.CreatePlan(currentState, goalState, availableActions, options);

        private static Planner Shared => _shared ?? (_shared = new Planner());

        public Plan CreatePlan(
            WorldState currentState,
            WorldState goalState,
            IReadOnlyList<IAction> availableActions,
            int maxDepth = 10)
            => CreatePlan(currentState, goalState, availableActions, new PlannerOptions(maxDepth));

        public Plan CreatePlan(
            WorldState currentState,
            WorldState goalState,
            IReadOnlyList<IAction> availableActions,
            PlannerOptions options)
        {
            if (currentState == null || goalState == null || availableActions == null)
                return GOAP.Plan.Invalid;

            if (currentState.Satisfies(goalState))
                return GOAP.Plan.Empty;

            _openList.Clear();
            _closedSet.Clear();
            options = Normalize(options);

            var startState = RentState();
            startState.CopyFrom(goalState);

            var startNode = RentNode();
            startNode.Parent = null;
            startNode.Action = null;
            startNode.State = startState;
            startNode.G = 0f;
            startNode.H = Heuristic(currentState, startState);
            startNode.Depth = 0;
            _openList.Push(startNode);

            Plan result = GOAP.Plan.Invalid;
            int expandedNodes = 0;

            try
            {
                while (_openList.Count > 0)
                {
                    if (expandedNodes++ >= options.MaxExpandedNodes)
                        break;

                    var current = _openList.Pop();

                    if (_closedSet.Contains(current.State))
                        continue;
                    _closedSet.Add(current.State);

                    if (currentState.Satisfies(current.State))
                    {
                        result = BuildPlan(current);
                        break;
                    }

                    if (current.Depth >= options.MaxDepth)
                        continue;

                    for (int a = 0; a < availableActions.Count; a++)
                    {
                        var action = availableActions[a];
                        if (action == null)
                            continue;

                        if (options.CheckActionApplicability && !action.IsApplicable(currentState))
                            continue;

                        if (!HasRelevantEffect(action, current.State))
                            continue;

                        var childState = BuildChildState(current.State, action);
                        if (childState == null)
                            continue;

                        if (_closedSet.Contains(childState))
                            continue;

                        float g = current.G + action.Cost;
                        float h = Heuristic(currentState, childState);

                        var childNode = RentNode();
                        childNode.Parent = current;
                        childNode.Action = action;
                        childNode.State = childState;
                        childNode.G = g;
                        childNode.H = h;
                        childNode.Depth = current.Depth + 1;
                        _openList.Push(childNode);
                    }
                }

                return result;
            }
            finally
            {
                ReturnAll();
            }
        }

        private static PlannerOptions Normalize(PlannerOptions options)
        {
            if (options.MaxDepth < 0)
                options.MaxDepth = 0;
            if (options.MaxExpandedNodes <= 0)
                options.MaxExpandedNodes = 512;
            return options;
        }

        private static float Heuristic(WorldState current, WorldState needed)
        {
            int unmet = 0;
            for (int i = 0; i < needed.Count; i++)
            {
                var (key, value, op, _) = needed.GetFullEntry(i);
                if (!current.TryGet(key, out var val) || !WorldState.CompareValues(val, op, value))
                    unmet++;
            }
            return unmet;
        }

        private Plan BuildPlan(PlannerNode goalNode)
        {
            _planBuffer.Clear();
            float cost = 0f;
            var node = goalNode;
            while (node.Action != null)
            {
                _planBuffer.Add(node.Action);
                cost += node.Action.Cost;
                node = node.Parent;
            }
            _planBuffer.Reverse();
            if (_planBuffer.Count == 0)
                return global::GOAP.Plan.Empty;
            return new Plan(new List<IAction>(_planBuffer), cost);
        }

        private static bool HasRelevantEffect(IAction action, WorldState requiredState)
        {
            for (int i = 0; i < requiredState.Count; i++)
            {
                var (reqKey, reqValue, reqOp, _) = requiredState.GetFullEntry(i);

                for (int j = 0; j < action.Effects.Count; j++)
                {
                    var (effKey, effValue, _, effMode) = action.Effects.GetFullEntry(j);
                    if (effKey != reqKey) continue;

                    // Add 效果：同 key 即视为相关（乐观）
                    if (effMode == EffectMode.Add && effValue != 0)
                        return true;

                    // Assign 效果：检查赋值后是否满足条件
                    if (WorldState.CompareValues(effValue, reqOp, reqValue))
                        return true;
                }
            }
            return false;
        }

        private WorldState BuildChildState(WorldState parentState, IAction action)
        {
            var child = RentState();
            child.CopyFrom(parentState);

            for (int i = 0; i < action.Effects.Count; i++)
            {
                var (effKey, effValue, _, effMode) = action.Effects.GetFullEntry(i);

                if (!child.TryGetFull(effKey, out var condValue, out var condOp, out _))
                    continue;

                if (effMode == EffectMode.Add)
                {
                    // 增减效果：乐观移除条件，运行时由 PlanExecutor 验证
                    child.Set(effKey, condValue - effValue, condOp);
                }
                else
                {
                    // 赋值效果：判断赋值后是否满足条件
                    if (WorldState.CompareValues(effValue, condOp, condValue))
                        child.Remove(effKey);
                }
            }
            if (!ApplyRequirements(child, action.Preconditions))
            {
                return null;
            }
            return child;
        }

        private static bool ApplyRequirements(WorldState target, WorldState requirements)
        {
            for (int i = 0; i < requirements.Count; i++)
            {
                var (key, value, op, mode) = requirements.GetFullEntry(i);
                if (!target.TryGetFull(key, out var existingValue, out var existingOp, out var existingMode))
                {
                    target.Set(key, value, op, mode);
                    continue;
                }

                if (TryMergeRequirements(existingValue, existingOp, value, op, out var mergedValue, out var mergedOp))
                    target.Set(key, mergedValue, mergedOp, existingMode);
                else
                    return false;
            }
            return true;
        }

        private static bool TryMergeRequirements(
            int leftValue,
            CompareOp leftOp,
            int rightValue,
            CompareOp rightOp,
            out int value,
            out CompareOp op)
        {
            value = leftValue;
            op = leftOp;

            if (leftOp == CompareOp.Equal)
            {
                if (!WorldState.CompareValues(leftValue, rightOp, rightValue))
                    return false;
                return true;
            }

            if (rightOp == CompareOp.Equal)
            {
                if (!WorldState.CompareValues(rightValue, leftOp, leftValue))
                    return false;
                value = rightValue;
                op = rightOp;
                return true;
            }

            if (IsLowerBound(leftOp) && IsLowerBound(rightOp))
            {
                if (rightValue > leftValue || (rightValue == leftValue && rightOp == CompareOp.Greater))
                {
                    value = rightValue;
                    op = rightOp;
                }
                else if (rightValue == leftValue && leftOp == CompareOp.Greater)
                {
                    op = leftOp;
                }
                return true;
            }

            if (IsUpperBound(leftOp) && IsUpperBound(rightOp))
            {
                if (rightValue < leftValue || (rightValue == leftValue && rightOp == CompareOp.Less))
                {
                    value = rightValue;
                    op = rightOp;
                }
                else if (rightValue == leftValue && leftOp == CompareOp.Less)
                {
                    op = leftOp;
                }
                return true;
            }

            if (IsLowerBound(leftOp) && IsUpperBound(rightOp))
                return false;

            if (IsUpperBound(leftOp) && IsLowerBound(rightOp))
                return false;

            return false;
        }

        private static bool IsLowerBound(CompareOp op)
            => op == CompareOp.Greater || op == CompareOp.GreaterOrEqual;

        private static bool IsUpperBound(CompareOp op)
            => op == CompareOp.Less || op == CompareOp.LessOrEqual;

        private PlannerNode RentNode()
        {
            var node = _nodePool.Count > 0 ? _nodePool.Pop() : new PlannerNode();
            _allocated.Add(node);
            return node;
        }

        private WorldState RentState()
        {
            var state = _statePool.Count > 0 ? _statePool.Pop() : new WorldState();
            state.Clear();
            _allocatedStates.Add(state);
            return state;
        }

        private void ReturnAll()
        {
            _openList.Clear();
            _closedSet.Clear();

            for (int i = 0; i < _allocated.Count; i++)
            {
                var node = _allocated[i];
                node.Parent = null;
                node.Action = null;
                node.State = null;
                _nodePool.Push(node);
            }
            _allocated.Clear();

            for (int i = 0; i < _allocatedStates.Count; i++)
            {
                var state = _allocatedStates[i];
                state.Clear();
                _statePool.Push(state);
            }
            _allocatedStates.Clear();
        }

        private sealed class WorldStateComparer : IEqualityComparer<WorldState>
        {
            public static readonly WorldStateComparer Instance = new WorldStateComparer();

            public bool Equals(WorldState x, WorldState y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.ExactEquals(y);
            }

            public int GetHashCode(WorldState obj) => obj.ComputeHash();
        }
    }

    internal class PlannerNode
    {
        public PlannerNode Parent;
        public IAction Action;
        public WorldState State;
        public float G;
        public float H;
        public int Depth;
        public float F => G + H;
    }
}
