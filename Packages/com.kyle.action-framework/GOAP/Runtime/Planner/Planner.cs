using System.Collections.Generic;

namespace GOAP
{
    // 规划器，使用反向 A* 搜索找到从当前状态到目标状态的最优行动序列
    // 反向搜索：从目标状态出发，反推每个行动的前置条件，直到当前状态满足所有前置条件
    public static class Planner
    {
        // 规划入口
        // currentState: 当前世界状态快照
        // goalState:    目标终态（只需满足其中的键值对即可）
        // availableActions: NPC 当前可用的行动列表
        // maxDepth:     最大搜索深度，防止无限递归
        // 返回 null 表示无解
        public static Plan Plan(
            WorldState currentState,
            WorldState goalState,
            IReadOnlyList<IAction> availableActions,
            int maxDepth = 10)
        {
            var openList = new List<PlannerNode>();
            var closedList = new List<PlannerNode>();

            // 起始节点：目标状态就是起点（反向搜索）
            var start = new PlannerNode
            {
                Parent = null,
                Action = null,
                State = goalState.Clone(),
                Cost = 0f
            };
            openList.Add(start);

            while (openList.Count > 0)
            {
                // 取代价最低的节点
                var current = GetLowestCostNode(openList);
                openList.Remove(current);
                closedList.Add(current);

                // 如果当前状态已满足（即当前世界状态满足了反向推导到此节点的所有前置条件）
                if (currentState.Satisfies(current.State))
                {
                    return BuildPlan(current);
                }

                // 搜索深度限制
                if (GetDepth(current) >= maxDepth)
                    continue;

                // 遍历所有可用行动，找能推进规划的行动
                foreach (var action in availableActions)
                {
                    // 行动运行时可行性检查
                    if (!action.IsAchievable(currentState))
                        continue;

                    // 反向逻辑：此行动的 Effects 是否满足当前节点需要的某些条件
                    if (!HasRelevantEffect(action, current.State))
                        continue;

                    // 生成子节点：将 current.State 中被 action.Effects 满足的条件移除，加入 action.Preconditions
                    var childState = BuildChildState(current.State, action);

                    var child = new PlannerNode
                    {
                        Parent = current,
                        Action = action,
                        State = childState,
                        Cost = current.Cost + action.Cost
                    };

                    // 跳过已在 closed 中的等价节点
                    if (IsInList(closedList, child))
                        continue;

                    openList.Add(child);
                }
            }

            // 无解
            return null;
        }

        // 从目标节点反向回溯，构建有序行动序列
        private static Plan BuildPlan(PlannerNode goalNode)
        {
            var actions = new List<IAction>();
            float cost = 0f;
            var node = goalNode;
            while (node.Action != null)
            {
                actions.Insert(0, node.Action);
                cost += node.Action.Cost;
                node = node.Parent;
            }
            return new Plan(actions, cost);
        }

        // 反向搜索：判断 action.Effects 中是否有键值对出现在 requiredState 中（且值相同）
        private static bool HasRelevantEffect(IAction action, WorldState requiredState)
        {
            foreach (var kvp in requiredState.State)
            {
                if (action.Effects.TryGet<object>(kvp.Key, out var effectVal))
                {
                    if (System.Object.Equals(effectVal, kvp.Value))
                        return true;
                }
            }
            return false;
        }

        // 构建子节点的 WorldState：
        // 从 parentState 中移除被 action.Effects 满足的键值对，再加入 action.Preconditions
        private static WorldState BuildChildState(WorldState parentState, IAction action)
        {
            var child = parentState.Clone();
            // 移除被 effects 满足的键（这些条件已被满足，不再需要规划）
            foreach (var kvp in action.Effects.State)
            {
                if (child.TryGet<object>(kvp.Key, out var val) && System.Object.Equals(val, kvp.Value))
                    child.Remove(kvp.Key);
            }
            // 加入前置条件（这些条件还需要被满足）
            child.Apply(action.Preconditions);
            return child;
        }

        private static PlannerNode GetLowestCostNode(List<PlannerNode> nodes)
        {
            var best = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].Cost < best.Cost)
                    best = nodes[i];
            }
            return best;
        }

        private static int GetDepth(PlannerNode node)
        {
            int depth = 0;
            var cur = node;
            while (cur.Parent != null) { depth++; cur = cur.Parent; }
            return depth;
        }

        private static bool IsInList(List<PlannerNode> list, PlannerNode node)
        {
            foreach (var n in list)
            {
                if (n.Action == node.Action && StatesEqual(n.State, node.State))
                    return true;
            }
            return false;
        }

        private static bool StatesEqual(WorldState a, WorldState b)
        {
            return a.Satisfies(b) && b.Satisfies(a);
        }
    }

    // 规划器内部搜索节点
    internal class PlannerNode
    {
        public PlannerNode Parent;
        public IAction Action;
        public WorldState State;   // 反向搜索中此节点仍需满足的条件集合
        public float Cost;
    }
}
