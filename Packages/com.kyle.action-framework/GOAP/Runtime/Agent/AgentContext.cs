namespace GOAP
{
    public readonly struct AgentContext
    {
        public readonly Agent Agent;
        public WorldState WorldState => Agent.WorldState;

        internal AgentContext(Agent agent)
        {
            Agent = agent;
        }
    }
}
