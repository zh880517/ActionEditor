namespace Flow
{
    public class FlowNodeRuntimeData
    {

    }

    public class TFlowNodeRuntimeData<T> : FlowNodeRuntimeData where T : struct
    {
        public T Value;
    }
}
