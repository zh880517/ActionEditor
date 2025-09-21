namespace Flow
{
    public class FlowRuntimeDebuger
    {
        const string DebugDefine = "FLOW_GRAPH_DEBUG";
        [System.Diagnostics.Conditional(DebugDefine)]
        public void OnNodeStart(long nodeId, int frameIndex)
        {
            OnNodeData(new FlowDebugNodeData { Type = FlowDebugNodeType.Execute, FrameIndex = frameIndex, NodeID = nodeId });
        }
        [System.Diagnostics.Conditional(DebugDefine)]
        public void OnNodeOutput(long nodeId, int outputIndex, int frameIndex)
        {
            OnNodeData(new FlowDebugNodeData { Type = FlowDebugNodeType.Output, FrameIndex = frameIndex, NodeID = nodeId, OutputIndex = outputIndex });
        }
        [System.Diagnostics.Conditional(DebugDefine)]
        public void OnDataNode(long nodeId, int frameIndex)
        {
            OnNodeData(new FlowDebugNodeData { Type = FlowDebugNodeType.ExecuteDataNode, FrameIndex = frameIndex, NodeID = nodeId });
        }

        [System.Diagnostics.Conditional(DebugDefine)]
        public void OnNodeParamChange(ulong edgeID, string value, int frameIndex)
        {
            OnNodeData(new FlowDebugNodeData { Type = FlowDebugNodeType.OutputValue, FrameIndex = frameIndex, EdgeID = edgeID, OutputValue = value });
        }

        protected virtual void OnNodeData(FlowDebugNodeData data) { }
    }
}
