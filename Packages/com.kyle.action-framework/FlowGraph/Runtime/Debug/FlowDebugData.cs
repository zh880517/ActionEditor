namespace Flow
{
    public struct FlowDeugGraphData
    {
        public string Name;
        public string DebugKey;
    }

    public enum FlowDebugNodeType
    {
        Execute = 0,
        ExecuteDataNode = 2,
        Output = 2,
        OutputValue = 3,
    }

    public struct FlowDebugNodeData
    {
        public FlowDebugNodeType Type;
        public int FrameIndex;
        public int NodeID;
        public int OutputIndex;
        public ulong EdgeID;
        public string OutputValue;
    }
}
