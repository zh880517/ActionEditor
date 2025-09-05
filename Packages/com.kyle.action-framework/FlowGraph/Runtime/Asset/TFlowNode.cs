namespace Flow
{
    public class TFlowNode<T> : FlowNode where T : struct
    {
        [ExpandedInParentAttribute]
        public T Value;
    }
}
