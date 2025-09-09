namespace Flow
{
    public class TFlowNode<T> : FlowNode where T : struct
    {
        [ExpandedInParent]
        public T Value;

        public override bool IsDefine<U>()
        {
            return Value is U;
        }
    }
}
