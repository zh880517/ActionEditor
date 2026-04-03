using System.Collections.Generic;
using UnityEngine;

namespace Flow
{
    /// <summary>
    /// 子图输入节点，代表子图的所有输入端口。
    /// 每个输入端口在此节点上表现为一个输出数据端口（数据从此节点流出到子图内部节点）。
    /// 端口定义来自 FlowSubGraph.InputPorts。
    /// </summary>
    public class SubGraphInputNode : FlowNode
    {
        /// <summary>
        /// 动态输出端口的EdgeID，key=portGUID
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
