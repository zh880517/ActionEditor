using System.Collections.Generic;
namespace Flow
{
    public class FlowGraphRuntimeContext
    {
        protected Dictionary<ulong, DynamicVariable> variables = new Dictionary<ulong, DynamicVariable>();
        protected FlowGraphRuntimeData runtimeData;
        protected int currentNodeIndex = -1;

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

        public bool TryGetValue<T>(int nodeID, int paramId, ref T value)
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

        public void SetValue<T>(OutputData<T> data, T value)
        {
            if(!variables.TryGetValue(data.Key, out var v))
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
                var result = executor.Execute(this, node);
                if(result.IsRunning)
                    break;
                currentNodeIndex = GetNextNodeIndex(node.NodeID, result.OutputIndex);
            }

            if (currentNodeIndex < 0)
            {
                Stop();
            }
        }

        protected int GetNextNodeIndex(int currentId, int portIndex)
        {
            foreach (var item in runtimeData.Edges)
            {
                if(item.OutputNodeID == currentId && item.OutputIndex == portIndex)
                {
                    return runtimeData.Nodes.FindIndex(n => n.NodeID == item.InputNodeID);
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
            currentNodeIndex = -1;
            NodeContext = null;
        }
    }
}
