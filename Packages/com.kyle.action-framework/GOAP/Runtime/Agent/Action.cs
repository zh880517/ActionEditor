namespace GOAP
{
    public sealed class RuntimeAction<T> : IAction where T : struct, IActionData
    {
        private enum ActionState { Inactive, Active }

        private readonly T _data;
        private readonly WorldState _preconditions;
        private readonly WorldState _effects;
        private ActionState _state = ActionState.Inactive;

        public RuntimeAction(SerializedActionData planningData, T executionData)
        {
            _data = executionData;
            Id = planningData.Id;
            Cost = planningData.Cost;
            _preconditions = WorldState.FromEntries(planningData.Preconditions);
            _effects = WorldState.FromEntries(planningData.Effects);
        }

        public string Id { get; }
        public float Cost { get; }
        public WorldState Preconditions => _preconditions;
        public WorldState Effects => _effects;

        public bool IsApplicable(WorldState current)
        {
            var runner = ActionRunner<T>.Runner;
            return runner == null || runner.IsApplicable(current, _data);
        }

        public void OnEnter(AgentContext ctx)
        {
            if (_state == ActionState.Active) return;
            _state = ActionState.Active;
            ActionRunner<T>.Runner?.OnEnter(ctx, _data);
        }

        public ActionStatus OnUpdate(AgentContext ctx, float deltaTime)
        {
            var runner = ActionRunner<T>.Runner;
            if (runner == null) return ActionStatus.Failed;
            return runner.OnUpdate(ctx, _data, deltaTime);
        }

        public void OnExit(AgentContext ctx)
        {
            if (_state != ActionState.Active) return;
            _state = ActionState.Inactive;
            ActionRunner<T>.Runner?.OnExit(ctx, _data);
        }

        public void OnAbort(AgentContext ctx)
        {
            if (_state != ActionState.Active) return;
            _state = ActionState.Inactive;
            ActionRunner<T>.Runner?.OnAbort(ctx, _data);
        }
    }
}
