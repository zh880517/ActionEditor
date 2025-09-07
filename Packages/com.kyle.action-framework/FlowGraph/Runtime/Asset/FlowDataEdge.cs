namespace Flow
{
    [System.Serializable]
    public class FlowDataEdge
    {
        public FlowNode Output;
        public FlowNode Input;
        public string OutputSlot;
        public string InputSlot;
        public ulong EdgeID;
    }
}
