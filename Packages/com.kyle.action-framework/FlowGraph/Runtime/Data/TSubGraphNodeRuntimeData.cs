namespace Flow
{
    /// <summary>
    /// TSubGraphNode&lt;TData, TGraph&gt; 的运行时数据，在 SubGraph 执行数据基础上额外携带 TData 值。
    /// </summary>
    public class TSubGraphNodeRuntimeData<TData> : SubGraphNodeRuntimeData
        where TData : struct
    {
        public TData Value;

        public override IFlowNodeExecutor Executor => TSubGraphNodeExecutor<TData>.Executor;
    }
}
