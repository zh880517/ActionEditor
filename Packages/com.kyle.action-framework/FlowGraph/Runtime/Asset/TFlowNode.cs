namespace Flow
{
    public class TFlowNode<T> : FlowNode where T : struct
    {
        [ExpandedInParent]
        public T Value;
    }
}
