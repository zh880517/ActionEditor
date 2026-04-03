using System.Collections.Generic;
using UnityEngine;

namespace Flow
{
    /// <summary>
    /// 子图输出节点，代表子图的所有输出端口。
    /// 每个输出端口在此节点上表现为一个输入数据端口（数据从子图内部节点流入此节点）。
    /// 端口定义来自 FlowSubGraph.OutputPorts。
    /// </summary>
    public class SubGraphOutputNode : FlowNode
    {
        /// <summary>
        /// 动态输入端口的EdgeID，key=portGUID
        /// </summary>
        [HideInInspector]
        public List<SubGraphPortEdge> PortEdges = new List<SubGraphPortEdge>();

        public override bool IsDefine<T>()
        {
            return false;
        }

        public override FlowNodeRuntimeData Export()
        {
            return null;
        }

        public ulong GetPortEdgeID(string portGUID)
        {
            foreach (var e in PortEdges)
                if (e.PortGUID == portGUID) return e.EdgeID;
            return 0;
        }

        public void SetPortEdgeID(string portGUID, ulong edgeID)
        {
            for (int i = 0; i < PortEdges.Count; i++)
            {
                if (PortEdges[i].PortGUID == portGUID)
                {
                    PortEdges[i] = new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID };
                    return;
                }
            }
            PortEdges.Add(new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID });
        }
    }
}
