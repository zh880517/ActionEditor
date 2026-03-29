using System.Collections.Generic;

namespace Flow
{
    [System.Serializable]
    public struct SubGraphPort
    {
        public string Name;
        public string GUID;
    }

    public class FlowSubGraph : FlowGraph
    {
        public List<SubGraphPort> InputPorts = new List<SubGraphPort>();
        public List<SubGraphPort> OutputPorts = new List<SubGraphPort>();

        /// <summary>
        /// 子图端口GUID -> 子图内部DataEdge的EdgeID（Input端口作为数据源）
        /// 在编辑器中连线时设置，运行时用于桥接父图数据
        /// </summary>
        public List<SubGraphPortEdge> InputPortEdges = new List<SubGraphPortEdge>();
        /// <summary>
        /// 子图端口GUID -> 子图内部DataEdge的EdgeID（Output端口作为数据接收）
        /// </summary>
        public List<SubGraphPortEdge> OutputPortEdges = new List<SubGraphPortEdge>();

        public ulong GetInputPortEdgeID(string portGUID)
        {
            foreach (var e in InputPortEdges)
                if (e.PortGUID == portGUID) return e.EdgeID;
            return 0;
        }

        public void SetInputPortEdgeID(string portGUID, ulong edgeID)
        {
            for (int i = 0; i < InputPortEdges.Count; i++)
            {
                if (InputPortEdges[i].PortGUID == portGUID)
                {
                    InputPortEdges[i] = new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID };
                    return;
                }
            }
            InputPortEdges.Add(new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID });
        }

        public ulong GetOutputPortEdgeID(string portGUID)
        {
            foreach (var e in OutputPortEdges)
                if (e.PortGUID == portGUID) return e.EdgeID;
            return 0;
        }

        public void SetOutputPortEdgeID(string portGUID, ulong edgeID)
        {
            for (int i = 0; i < OutputPortEdges.Count; i++)
            {
                if (OutputPortEdges[i].PortGUID == portGUID)
                {
                    OutputPortEdges[i] = new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID };
                    return;
                }
            }
            OutputPortEdges.Add(new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID });
        }
    }
}
