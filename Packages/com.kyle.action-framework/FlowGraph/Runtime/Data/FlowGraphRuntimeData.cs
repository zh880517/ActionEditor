using System.Collections.Generic;

namespace Flow
{
    public struct FlowRuntimeEdge
    {
        public int OutputNodeID;
        public int OutputIndex;
        public int InputNodeID;
    }

    public struct FlowRuntimeDataEdge
    {
        public int DataNodeID;//如果不为0，表示这个数据是从DataNode输出的,获取数据时需要保证这个节点执行过一次
        public ulong EdgeID;
        public ulong InputKey;// NodeID << 32 | Hash(FieldName)
    }

    public class FlowGraphRuntimeData
    {
        public List<FlowNodeRuntimeData> Nodes = new List<FlowNodeRuntimeData>();
        public List<FlowRuntimeEdge> Edges = new List<FlowRuntimeEdge>();
        public List<FlowRuntimeDataEdge> DataEdges = new List<FlowRuntimeDataEdge>();
        public string Name;
    }
}
