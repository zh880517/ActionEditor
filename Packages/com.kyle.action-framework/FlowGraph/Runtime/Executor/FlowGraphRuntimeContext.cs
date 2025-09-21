using System.Collections.Generic;
namespace Flow
{
    public class FlowGraphRuntimeContext
    {
        protected readonly Dictionary<ulong, DynamicVariable> variables = new Dictionary<ulong, DynamicVariable>();
        protected readonly HashSet<int> cachedDataNodeIndexs = new HashSet<int>();
        protected FlowGraphRuntimeData runtimeData;
        protected int currentNodeIndex = -1;
        protected int depenceIndex = -1;
        private readonly string debugKey;//调试标识,用于在编辑中查找执行脚本的角色的位移标识
        private FlowRuntimeDebuger debuger;
        public int RuningFrame { get; private set; }
        public bool IsRunning => currentNodeIndex >= 0;

        public FlowGraphRuntimeContext(FlowGraphRuntimeData data, string debugKey)
        {
            runtimeData = data;
            this.debugKey = debugKey;
        }

        public UpdateNodeContext NodeContext { get; private set; }
        public void SetNodeContext(UpdateNodeContext context)
        {
            NodeContext = context;
        }

        public bool TryGetInputValue<T>(int nodeID, int paramId, ref T value)
        {
            ulong key = ((ulong)nodeID << 32) | (uint)paramId;
            if(runtimeData.InputKeyToEdgeID.TryGetValue(key, out ulong edgeID))
            {
                if (variables.TryGetValue(edgeID, out var v))
                {
                    var dv = v as TDynamicVariable<T>;
                    value = dv.Value;
                    return true;
                }
            }
            return false;
        }

        public void SetOutputValue<T>(OutputData<T> data, T value)
        {
            // Key为0表示没有使用该输出
            if (data.Key == 0)
                return;
            //调试
            debuger?.OnNodeParamChange(data.Key, value?.ToString(), RuningFrame);
            if (!variables.TryGetValue(data.Key, out var v))
            {
                var dv = TDynamicVariable<T>.Get();
                dv.Value = value;
                variables.Add(data.Key, dv);
            }
            else
            {
                var dv = v as TDynamicVariable<T>;
                dv.Value = value;
            }
        }

        public virtual void Start()
        {
            RuningFrame = 0;
            debuger = FlowDebugContext.CreateDebuger(runtimeData.Name, debugKey);
            SetCurrentNode(runtimeData.Nodes[0].NodeID);
        }

        public void Update()
        {
            while (currentNodeIndex >= 0 && currentNodeIndex < runtimeData.Nodes.Count)
            {
                var node = runtimeData.Nodes[currentNodeIndex];
                var executor = node.Executor;
                if (executor == null)
                {
                    UnityEngine.Debug.LogError($"Node {node.GetType().Name} executor is null");
                    currentNodeIndex = -1;
                    break;
                }
                //执行依赖的数据节点
                if(depenceIndex > 0)
                {
                    var dep = runtimeData.DataNodeDependencies[depenceIndex];
                    foreach (var item in dep.Dependencies)
                    {
                        if (cachedDataNodeIndexs.Contains(item))
                            continue;
                        var depNode = runtimeData.Nodes[item];
                        //调试
                        debuger?.OnDataNode(GetNodeUID(depNode.NodeID), RuningFrame);

                        var depExecutor = depNode.Executor;
                        depExecutor.Execute(this, depNode);
                        if(depNode.IsRealTimeData)
                            cachedDataNodeIndexs.Add(item);
                    }
                }

                //节点执行
                //调试
                if (NodeContext == null)
                    debuger?.OnNodeStart(GetNodeUID(node.NodeID), RuningFrame);

                var result = executor.Execute(this, node);
                if(result.IsRunning)
                    break;
                //调试
                debuger?.OnNodeOutput(GetNodeUID(node.NodeID), result.OutputIndex, RuningFrame);
                
                var nextId = GetNextNodeID(node.NodeID, result.OutputIndex);
                SetCurrentNode(nextId);
            }
            RuningFrame++;
            if (currentNodeIndex < 0)
            {
                Stop();
            }
        }

        protected void SetCurrentNode(int nodeId)
        {
            currentNodeIndex = runtimeData.Nodes.FindIndex(n => n.NodeID == nodeId);
            depenceIndex = runtimeData.DataNodeDependencies.FindIndex(n => n.NodeID == nodeId);
        }

        protected int GetNextNodeID(int currentId, int portIndex)
        {
            foreach (var item in runtimeData.Edges)
            {
                if(item.OutputNodeID == currentId && item.OutputIndex == portIndex)
                {
                    return item.InputNodeID;
                }
            }
            return -1;
        }

        protected long GetNodeUID(int nodeId)
        {
            if (runtimeData.NodeUIDs.TryGetValue(nodeId, out long uid))
                return uid;
            return -1;
        }

        public virtual void Stop()
        {
            foreach (var item in variables)
            {
                item.Value.Recyle();
            }
            variables.Clear();
            cachedDataNodeIndexs.Clear();
            currentNodeIndex = -1;
            NodeContext = null;
            FlowDebugContext.Stop(debuger);
            debuger = null;
        }
    }
}
