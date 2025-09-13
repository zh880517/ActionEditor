namespace Flow
{
    public class FlowGraphRuntimeContext
    {
        public UpdateNodeContext NodeContext { get; private set; }
        public void SetNodeContext(UpdateNodeContext context)
        {
            NodeContext = context;
        }

        public bool TryGetValue<T>(int nodeID, int paramId, ref T value)
        {
            ulong key = ((ulong)nodeID << 32) | (uint)paramId;
            return false;
        }
    }
}
