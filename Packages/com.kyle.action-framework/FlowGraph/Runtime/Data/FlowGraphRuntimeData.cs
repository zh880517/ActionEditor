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
        public ulong EdgeID;
        public ulong InputKey;// NodeID << 32 | Hash(FieldName)
    }

    //数据输入端口依赖的数据提供端口
    // 用于运行时数据节点初始化时，指定数据提供端口节点的执行顺序
    public struct FlowDataNodeDependency
    {
        public int NodeID;
        public int DataNodeID;
    }

    public class FlowGraphRuntimeData
    {
        public List<FlowNodeRuntimeData> Nodes = new List<FlowNodeRuntimeData>();
        public List<FlowRuntimeEdge> Edges = new List<FlowRuntimeEdge>();
        public List<FlowRuntimeDataEdge> DataEdges = new List<FlowRuntimeDataEdge>();
        public List<FlowDataNodeDependency> DataNodeDependencies = new List<FlowDataNodeDependency>();
        public string Name;
    }
}
