using System.Collections.Generic;

namespace Flow
{
    public struct FlowRuntimeEdge
    {
        public int OutputNodeID;
        public int OutputIndex;
        public int InputNodeID;
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
        public List<FlowNodeRuntimeData> Nodes = new List<FlowNodeRuntimeData>();//第一个节点必须是入口节点的下一个节点
        public List<FlowRuntimeEdge> Edges = new List<FlowRuntimeEdge>();
        public Dictionary<ulong, ulong> InputKeyToEdgeID = new Dictionary<ulong, ulong>();// InputKey -> EdgeID
        public List<FlowDataNodeDependency> DataNodeDependencies = new List<FlowDataNodeDependency>();
        public string Name;
    }
}
