namespace Flow
{
    public class SubGraphNodeExecutor : IFlowNodeExecutor
    {
        public static readonly SubGraphNodeExecutor Instance = new SubGraphNodeExecutor();

        public FlowNodeResult Execute(FlowGraphRuntimeContext context, FlowNodeRuntimeData data)
        {
            var subData = data as SubGraphNodeRuntimeData;
            if (subData == null || string.IsNullOrEmpty(subData.SubGraphName))
                return FlowNodeResult.Next;

            var subGraphRuntimeData = FlowSubGraphProvider.Get(subData.SubGraphName);
            if (subGraphRuntimeData == null)
            {
                UnityEngine.Debug.LogError($"SubGraph '{subData.SubGraphName}' not found via FlowSubGraphProvider");
                return FlowNodeResult.Next;
            }

            // 创建子图运行上下文，同步执行（阻塞父图）
            var subContext = new SubGraphRuntimeContext(subGraphRuntimeData, subData.SubGraphName, context, subData);
            subContext.Start();
            while (subContext.IsRunning)
            {
                subContext.Update();
            }
            return FlowNodeResult.Next;
        }
    }
}
