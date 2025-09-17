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
            if(HasInput)
            {
                var value = runtimeData.Value;
                FillInputs(context as TContext, data.NodeID, ref value);
                return Execute(context as TContext, value);
            }
            else
            {
                return Execute(context as TContext, runtimeData.Value);
            }
        }
        protected abstract FlowNodeResult Execute(TContext context, T data);

        //下面两个接口子类代码自动生成
        protected virtual bool HasInput => false;
        protected virtual void FillInputs(TContext context, int nodeId, ref T data)
        {
        }
    }

    public abstract class TNormalExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            OnExecute(context, data);
            return FlowNodeResult.Next;
        }
        protected abstract void OnExecute(TContext context, T data);
    }

    public abstract class TDynamicOutputExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            int index = Select(context, data);
            return new FlowNodeResult { IsRunning = false, OutputIndex = index };
        }
        protected abstract int Select(TContext context, T data);
    }

    public abstract class TConditionExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct, IFlowConditionable
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            return OnCondition(context, data) ? FlowNodeResult.True : FlowNodeResult.False;
        }
        protected abstract bool OnCondition(TContext context, T data);
    }

    public abstract class TUpdateableExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct, IFlowUpdateable
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            var nodeContext = context.NodeContext;
            if(nodeContext == null)
            {
                nodeContext = CreateNodeContext();
                OnEnter(context, data, nodeContext);
            }
            if (!OnUpdate(context, data, nodeContext))
            {
                OnExit(context, data, nodeContext);
                return FlowNodeResult.Next;
            }
            return FlowNodeResult.Running;
        }

        protected virtual UpdateNodeContext CreateNodeContext() => UpdateNodeContext.None;
        protected virtual void OnEnter(TContext context, T data, UpdateNodeContext nodeContext) { }
        protected abstract bool OnUpdate(TContext context, T data, UpdateNodeContext nodeContext);
        protected virtual void OnExit(TContext context, T data, UpdateNodeContext nodeContext) { }
    }


    public abstract class TConditionUpdateableExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct, IFlowUpdateable, IFlowConditionable
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        public enum ResultType
        {
            Running,
            True,
            False
        }
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            var nodeContext = context.NodeContext;
            if (nodeContext == null)
            {
                nodeContext = CreateNodeContext();
                OnEnter(context, data, nodeContext);
            }
            var result = OnUpdate(context, data, nodeContext);
            if (result == ResultType.Running)
            {
                return FlowNodeResult.Running;
            }
            OnExit(context, data, nodeContext);
            if (result == ResultType.True)
                return FlowNodeResult.True;
            else
                return FlowNodeResult.False;
        }

        protected virtual UpdateNodeContext CreateNodeContext() => UpdateNodeContext.None;
        protected virtual void OnEnter(TContext context, T data, UpdateNodeContext nodeContext) { }
        protected abstract ResultType OnUpdate(TContext context, T data, UpdateNodeContext nodeContext);
        protected virtual void OnExit(TContext context, T data, UpdateNodeContext nodeContext) { }
    }

    public abstract class TDynamicOutputUpdateableExecutor<T, TRuntimeData, TContext> : TFlowNodeExecutor<T, TRuntimeData, TContext>
        where T : struct, IFlowUpdateable, IFlowDynamicOutputable
        where TRuntimeData : TFlowNodeRuntimeData<T>
        where TContext : FlowGraphRuntimeContext
    {
        protected const int Running = -1;
        protected override FlowNodeResult Execute(TContext context, T data)
        {
            var nodeContext = context.NodeContext;
            if (nodeContext == null)
            {
                nodeContext = CreateNodeContext();
                OnEnter(context, data, nodeContext);
            }
            var portIndex = OnUpdate(context, data, nodeContext);
            if (portIndex == Running)
            {
                return FlowNodeResult.Running;
            }
            OnExit(context, data, nodeContext);
            return new FlowNodeResult { IsRunning = false, OutputIndex = portIndex };
        }

        protected virtual UpdateNodeContext CreateNodeContext() => UpdateNodeContext.None;
        protected virtual void OnEnter(TContext context, T data, UpdateNodeContext nodeContext) { }
        protected abstract int OnUpdate(TContext context, T data, UpdateNodeContext nodeContext);
        protected virtual void OnExit(TContext context, T data, UpdateNodeContext nodeContext) { }
    }
}
