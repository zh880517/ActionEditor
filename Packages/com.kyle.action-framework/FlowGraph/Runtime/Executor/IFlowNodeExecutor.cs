namespace Flow
{
    public interface IFlowNodeExecutor
    {
        FlowNodeResult Execute(FlowGraphRuntimeContext context, FlowNodeRuntimeData data);
    }
}
