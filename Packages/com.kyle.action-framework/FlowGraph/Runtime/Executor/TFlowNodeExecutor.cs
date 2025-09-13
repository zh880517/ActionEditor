namespace Flow
{
    public abstract class TFlowNodeExecutor<T, TRuntimeData, TContext> : IFlowNodeExecutor 
        where T : struct
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext :FlowGraphRuntimeContext 
    {
        public FlowNodeResult Execute(FlowGraphRuntimeContext context, FlowNodeRuntimeData data)
        {
            var runtimeData = data as TRuntimeData;
            var value = runtimeData.Value;
            FillInputs(context as TContext, data.NodeID, ref value);
            return Execute(context as TContext, value);
        }
        protected abstract FlowNodeResult Execute(TContext context, T data);

        protected virtual void FillInputs(TContext context, int nodeId, ref T data)
        {
        }
    }

    public abstract class TActionFlowNodeExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            OnAction(context, data);
            return FlowNodeResult.Next;
        }
        protected abstract void OnAction(TContext context, T data);
    }

    public abstract class TConditionFlowNodeExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            return OnCondition(context, data) ? FlowNodeResult.True : FlowNodeResult.False;
        }
        protected abstract bool OnCondition(TContext context, T data);
    }

    public abstract class TUpdateableFlowNodeExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected const int Running = -1;
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            var nodeContext = context.NodeContext;
            if(nodeContext == null)
            {
                nodeContext = CreateNodeContext();
                OnEnter(context, data, nodeContext);
            }
            int outputIndex = OnUpdate(context, data, nodeContext);
            if (outputIndex == Running)
            {
                OnExit(context, data, nodeContext);
                return FlowNodeResult.Running;
            }
            return new FlowNodeResult { IsRunning = false, OutputIndex = outputIndex };
        }

        protected virtual UpdateNodeContext CreateNodeContext() => UpdateNodeContext.None;
        protected virtual void OnEnter(TContext context, T data, UpdateNodeContext nodeContext) { }
        protected abstract int OnUpdate(TContext context, T data, UpdateNodeContext nodeContext);
        protected virtual void OnExit(TContext context, T data, UpdateNodeContext nodeContext) { }
    }
}
