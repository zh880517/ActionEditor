namespace Flow
{
    [System.Serializable]
    public class FlowDataEdge
    {
        public FlowNode From;
        public FlowNode To;
        public string FromSlot;
        public string ToSlot;
    }
}
