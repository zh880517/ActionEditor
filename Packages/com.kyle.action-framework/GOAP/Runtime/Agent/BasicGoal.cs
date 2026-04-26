namespace GOAP
{
    // 从 GoalRuntimeData 直接初始化的非泛型具体目标实现
    // 由 AgentFactory 使用；优先级与有效性均为静态默认值
    // 需要动态逻辑的目标请继承 Goal<T> 并重写 OnGetPriority / OnIsValid
    public class BasicGoal : IGoal
    {
        private readonly GoalRuntimeData _data;
        private readonly WorldState _desiredState;

        public BasicGoal(GoalRuntimeData data)
        {
            _data = data;
            _desiredState = WorldState.FromEntries(data.DesiredState);
        }

        public string Id => _data.Id;
        public float InsistenceBias => 0f;
        public float GetPriority(WorldState current) => _data.BasePriority;
        public WorldState GetDesiredState() => _desiredState;
        public bool IsValid(WorldState current) => true;
    }
}
