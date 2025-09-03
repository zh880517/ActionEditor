namespace Flow
{
    [System.Serializable]
    public class FlowEdge
    {
        public FlowNode Output;//输出节点
        public FlowNode Input;//目标节点
        public int OutputIndex;
    }
}
