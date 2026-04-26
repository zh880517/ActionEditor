using System.Collections.Generic;

namespace GOAP
{
    public static class Planner
    {
        private static readonly Stack<PlannerNode> _nodePool = new Stack<PlannerNode>(64);
        private static readonly Stack<WorldState> _statePool = new Stack<WorldState>(64);
        private static readonly List<PlannerNode> _allocated = new List<PlannerNode>(64);
        private static readonly List<WorldState> _allocatedStates = new List<WorldState>(64);

        private static readonly MinHeap<PlannerNode> _openList =
            new MinHeap<PlannerNode>((a, b) => a.F.CompareTo(b.F));

        private static readonly HashSet<WorldState> _closedSet =
            new HashSet<WorldState>(WorldStateComparer.Instance);

        private static readonly List<IAction> _planBuffer = new List<IAction>();

        public static Plan Plan(
            WorldState currentState,
            WorldState goalState,
            IReadOnlyList<IAction> availableActions,
            int maxDepth = 10)
        {
            _openList.Clear();
            _closedSet.Clear();

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

            Plan result = GOAP.Plan.Empty;

            while (_openList.Count > 0)
            {
                var current = _openList.Pop();

                if (_closedSet.Contains(current.State))
                    continue;
                _closedSet.Add(current.State);

                if (currentState.Satisfies(current.State))
                {
                    result = BuildPlan(current);
                    break;
                }

                if (current.Depth >= maxDepth)
                    continue;

                for (int a = 0; a < availableActions.Count; a++)
                {
                    var action = availableActions[a];

                    if (!action.IsApplicable(currentState))
                        continue;

                    if (!HasRelevantEffect(action, current.State))
                        continue;

                    var childState = BuildChildState(current.State, action);

                    if (_closedSet.Contains(childState))
                    {
                        ReturnState(childState);
                        continue;
                    }

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

            ReturnAll();
            return result;
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

        private static Plan BuildPlan(PlannerNode goalNode)
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
                    if (effMode == EffectMode.Add)
                        return true;

                    // Assign 效果：检查赋值后是否满足条件
                    if (WorldState.CompareValues(effValue, reqOp, reqValue))
                        return true;
                }
            }
            return false;
        }

        private static WorldState BuildChildState(WorldState parentState, IAction action)
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
                    child.Remove(effKey);
                }
                else
                {
                    // 赋值效果：判断赋值后是否满足条件
                    if (WorldState.CompareValues(effValue, condOp, condValue))
                        child.Remove(effKey);
                }
            }
            child.Apply(action.Preconditions);
            return child;
        }

        private static PlannerNode RentNode()
        {
            var node = _nodePool.Count > 0 ? _nodePool.Pop() : new PlannerNode();
            _allocated.Add(node);
            return node;
        }

        private static WorldState RentState()
        {
            var state = _statePool.Count > 0 ? _statePool.Pop() : new WorldState();
            state.Clear();
            _allocatedStates.Add(state);
            return state;
        }

        private static void ReturnState(WorldState state)
        {
            _allocatedStates.Remove(state);
            state.Clear();
            _statePool.Push(state);
        }

        private static void ReturnAll()
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
