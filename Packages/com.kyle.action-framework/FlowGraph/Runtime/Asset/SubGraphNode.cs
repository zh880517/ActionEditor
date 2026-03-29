using System.Collections.Generic;
using UnityEngine;

namespace Flow
{
    [System.Serializable]
    public struct SubGraphPortEdge
    {
        public string PortGUID;
        public ulong EdgeID;
    }

    public class SubGraphNode : FlowNode
    {
        public FlowSubGraph SubGraph;

        [HideInInspector]
        public List<SubGraphPortEdge> InputEdges = new List<SubGraphPortEdge>();
        [HideInInspector]
        public List<SubGraphPortEdge> OutputEdges = new List<SubGraphPortEdge>();

        public override bool IsDefine<T>()
        {
            return typeof(T) == typeof(IFlowInputable)
                || typeof(T) == typeof(IFlowOutputable)
                || typeof(T) == typeof(IFlowNode);
        }

        public override FlowNodeRuntimeData Export()
        {
            var data = new SubGraphNodeRuntimeData();
            if (SubGraph != null)
            {
                data.SubGraphName = SubGraph.name;
                foreach (var edge in InputEdges)
                {
                    if (edge.EdgeID != 0)
                        data.InputPortEdgeIDs[edge.PortGUID] = edge.EdgeID;
                }
                foreach (var edge in OutputEdges)
                {
                    if (edge.EdgeID != 0)
                        data.OutputPortEdgeIDs[edge.PortGUID] = edge.EdgeID;
                }
                // 收集子图内部端口连线的EdgeID（端口贴边连线存储在FlowSubGraph上）
                foreach (var pe in SubGraph.InputPortEdges)
                {
                    if (pe.EdgeID != 0)
                        data.InputPortSubEdgeIDs[pe.PortGUID] = pe.EdgeID;
                }
                foreach (var pe in SubGraph.OutputPortEdges)
                {
                    if (pe.EdgeID != 0)
                        data.OutputPortSubEdgeIDs[pe.PortGUID] = pe.EdgeID;
                }
            }
            return data;
        }

        public ulong GetInputEdgeID(string portGUID)
        {
            foreach (var edge in InputEdges)
            {
                if (edge.PortGUID == portGUID)
                    return edge.EdgeID;
            }
            return 0;
        }

        public void SetInputEdgeID(string portGUID, ulong edgeID)
        {
            for (int i = 0; i < InputEdges.Count; i++)
            {
                if (InputEdges[i].PortGUID == portGUID)
                {
                    InputEdges[i] = new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID };
                    return;
                }
            }
            InputEdges.Add(new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID });
        }

        public ulong GetOutputEdgeID(string portGUID)
        {
            foreach (var edge in OutputEdges)
            {
                if (edge.PortGUID == portGUID)
                    return edge.EdgeID;
            }
            return 0;
        }

        public void SetOutputEdgeID(string portGUID, ulong edgeID)
        {
            for (int i = 0; i < OutputEdges.Count; i++)
            {
                if (OutputEdges[i].PortGUID == portGUID)
                {
                    OutputEdges[i] = new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID };
                    return;
                }
            }
            OutputEdges.Add(new SubGraphPortEdge { PortGUID = portGUID, EdgeID = edgeID });
        }
    }
}
