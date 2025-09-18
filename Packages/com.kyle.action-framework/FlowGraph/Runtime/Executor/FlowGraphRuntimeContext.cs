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

        public bool IsRunning => currentNodeIndex >= 0;

        public FlowGraphRuntimeContext(FlowGraphRuntimeData data)
        {
            runtimeData = data;
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
            currentNodeIndex = 0;
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
                if(depenceIndex > 0)
                {
                    var dep = runtimeData.DataNodeDependencies[depenceIndex];
                    foreach (var item in dep.Dependencies)
                    {
                        if (cachedDataNodeIndexs.Contains(item))
                            continue;
                        var depNode = runtimeData.Nodes[item];
                        var depExecutor = depNode.Executor;
                        depExecutor.Execute(this, depNode);
                        if(depNode.IsRealTimeData)
                            cachedDataNodeIndexs.Add(item);
                    }
                }
                var result = executor.Execute(this, node);
                if(result.IsRunning)
                    break;
                var nextId = GetNextNodeID(node.NodeID, result.OutputIndex);
                SetCurrentNode(nextId);
            }

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
        }
    }
}
