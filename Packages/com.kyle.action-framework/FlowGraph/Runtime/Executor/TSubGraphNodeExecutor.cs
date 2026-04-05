namespace Flow
{
    /// <summary>
    /// TSubGraphNode&lt;TData, TGraph&gt; 的运行时执行器容器。
    /// 优先使用用户通过 FlowNodeExecutor&lt;TData&gt;.Register 注册的自定义执行器；
    /// 若未注册，则降级使用 SubGraphNodeExecutor（标准子图执行逻辑）。
    /// </summary>
    public static class TSubGraphNodeExecutor<TData> where TData : struct
    {
        public static IFlowNodeExecutor Executor => FlowNodeExecutor<TData>.Executor ?? SubGraphNodeExecutor.Instance;
    }
}
