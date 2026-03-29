namespace Flow
{
    /// <summary>
    /// 子图运行上下文，通过覆写TryGetInputValue/SetOutputValue直接桥接父子图数据。
    /// Input端口：子图内节点读取数据时，若edgeID匹配InputPortSubEdgeIDs，重定向到父图。
    /// Output端口：子图内节点写出数据时，若edgeID匹配OutputPortSubEdgeIDs，写到本地后Stop时传回父图。
    /// </summary>
    public class SubGraphRuntimeContext : FlowGraphRuntimeContext
    {
        private readonly FlowGraphRuntimeContext parentContext;
        private readonly SubGraphNodeRuntimeData subNodeData;

        public SubGraphRuntimeContext(
            FlowGraphRuntimeData data,
            string debugKey,
            FlowGraphRuntimeContext parentContext,
            SubGraphNodeRuntimeData subNodeData)
            : base(data, debugKey)
        {
            this.parentContext = parentContext;
            this.subNodeData = subNodeData;
        }

        public override bool TryGetInputValue<T>(int nodeID, int paramId, ref T value)
        {
            ulong key = ((ulong)nodeID << 32) | (uint)paramId;
            if (runtimeData.InputKeyToEdgeID.TryGetValue(key, out ulong edgeID))
            {
                // 检查是否是Input端口的子图内部EdgeID，重定向到父图变量池
                foreach (var kv in subNodeData.InputPortSubEdgeIDs)
                {
                    if (edgeID == kv.Value
                        && subNodeData.InputPortEdgeIDs.TryGetValue(kv.Key, out ulong parentEdgeID))
                    {
                        if (parentContext.variables.TryGetValue(parentEdgeID, out var v))
                        {
                            var dv = v as TDynamicVariable<T>;
                            if (dv == null)
                                return false;
                            value = dv.Value;
                            return true;
                        }
                        return false;
                    }
                }
            }
            // 非端口数据，走常规逻辑
            return base.TryGetInputValue(nodeID, paramId, ref value);
        }

        public override void Stop()
        {
            // 将子图Output端口数据写回父图变量池
            foreach (var kv in subNodeData.OutputPortSubEdgeIDs)
            {
                string portGUID = kv.Key;
                ulong subEdgeID = kv.Value;
                if (subNodeData.OutputPortEdgeIDs.TryGetValue(portGUID, out ulong parentEdgeID)
                    && variables.TryGetValue(subEdgeID, out var variable))
                {
                    if (parentContext.variables.TryGetValue(parentEdgeID, out var existing))
                    {
                        existing.Recyle();
                    }
                    parentContext.variables[parentEdgeID] = variable.Clone();
                }
            }
            base.Stop();
        }
    }
}
