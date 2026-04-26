namespace GOAP
{
    public abstract class TActionRunner<T> where T : struct, IActionData
    {
        public virtual bool IsApplicable(WorldState current, T data) => true;

        public virtual void OnEnter(AgentContext ctx, T data) { }

        public abstract ActionStatus OnUpdate(AgentContext ctx, T data, float deltaTime);

        public virtual void OnExit(AgentContext ctx, T data) { }

        public virtual void OnAbort(AgentContext ctx, T data) { }
    }
}
