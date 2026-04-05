namespace GOAP.EditorView
{
    // 连接运行时 Agent 和编辑器调试面板的桥接器
    // 游戏代码在 MonoBehaviour 中调用 DebugBridge.Register/Unregister
    // 编辑器调试面板订阅 OnAgentUpdated 事件获取最新状态快照
    public static class DebugBridge
    {
        // 当有 Agent 注册或更新时触发，携带当前 Agent 快照
        public static event System.Action<AgentSnapshot> OnAgentUpdated;

        // 注册一个 Agent，后续每次调用 NotifyUpdated 时触发事件
        public static void Register(Agent agent)
        {
            _trackedAgent = agent;
        }

        // 取消注册
        public static void Unregister(Agent agent)
        {
            if (_trackedAgent == agent)
                _trackedAgent = null;
        }

        // 游戏代码在 Agent.Tick() 后调用，推送当前状态到编辑器
        public static void NotifyUpdated()
        {
            if (_trackedAgent == null) return;
            var snapshot = new AgentSnapshot
            {
                Status = _trackedAgent.Status,
                CurrentGoalId = _trackedAgent.CurrentGoal?.Id,
                CurrentGoalPriority = _trackedAgent.CurrentGoal?.GetPriority(_trackedAgent.WorldState) ?? 0f,
                CurrentActionId = _trackedAgent.CurrentAction?.Id,
                PlanActionIds = BuildPlanIds(_trackedAgent.CurrentPlan),
                CurrentPlanIndex = GetCurrentPlanIndex(_trackedAgent),
                WorldStateSnapshot = _trackedAgent.WorldState.ToString(),
                LastPlanTimeMs = _trackedAgent.LastPlanTimeMs
            };
            OnAgentUpdated?.Invoke(snapshot);
        }

        private static Agent _trackedAgent;

        private static string[] BuildPlanIds(Plan plan)
        {
            if (plan == null || !plan.IsValid) return System.Array.Empty<string>();
            var ids = new string[plan.Actions.Count];
            for (int i = 0; i < plan.Actions.Count; i++)
                ids[i] = plan.Actions[i].Id;
            return ids;
        }

        private static int GetCurrentPlanIndex(Agent agent)
        {
            if (agent.CurrentAction == null || agent.CurrentPlan == null) return -1;
            for (int i = 0; i < agent.CurrentPlan.Actions.Count; i++)
            {
                if (agent.CurrentPlan.Actions[i] == agent.CurrentAction)
                    return i;
            }
            return -1;
        }
    }

    // Agent 状态快照，用于跨线程/帧传递调试数据
    public class AgentSnapshot
    {
        public AgentStatus Status;
        public string CurrentGoalId;
        public float CurrentGoalPriority;
        public string CurrentActionId;
        public string[] PlanActionIds;
        public int CurrentPlanIndex;
        public string WorldStateSnapshot;
        public float LastPlanTimeMs;
    }
}
