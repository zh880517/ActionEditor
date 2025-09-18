namespace Flow
{
    public abstract class FlowNodeRuntimeData
    {
        public int NodeID;//节点在Graph的Nodes中索引，方便调试器与编辑器中的节点关联
        public abstract IFlowNodeExecutor Executor { get; }

        //仅纯数据节点有效
        public virtual bool IsRealTimeData => false;
    }

    public class TFlowNodeRuntimeData<T> : FlowNodeRuntimeData where T : struct
    {
        public override IFlowNodeExecutor Executor => FlowNodeExecutor<T>.Executor;
        public override bool IsRealTimeData => Value is IFlowDataProvider provider && provider.IsRealTimeData;

        public T Value;
    }
}
