using System.Collections.Generic;
using System.Linq;

namespace Flow
{
    public static class FlowGraphExport
    {
        public static void Export(FlowGraph graph, FlowGraphRuntimeData exportData)
        {
            exportData.Name = graph.name;
            var entryNode = graph.Nodes.FirstOrDefault(n => n.IsDefine<IFlowEntry>());
            if (entryNode == null)
            {
                throw new System.Exception("FlowGraph must have an Entry node");
            }
            int index = graph.Edges.FindIndex(it => it.Output == entryNode);
            if (index < 0)
            {
                throw new System.Exception("Entry node must have an output edge");
            }
            var startNode = graph.Edges[index].Input;
            if (startNode == null)
            {
                throw new System.Exception("Entry node must have an output edge");
            }
            Queue<FlowNode> nodes = new Queue<FlowNode>();
            nodes.Enqueue(startNode);
            while (nodes.Count > 0)
            {
                var first = nodes.Dequeue();
                CollectNode(graph, first, exportData, nodes);
            }
        }

        public static void CollectNode(FlowGraph graph, FlowNode node, FlowGraphRuntimeData exportData, Queue<FlowNode> nodes)
        {
            int nodeID = graph.Nodes.IndexOf(node);
            if (exportData.Nodes.Exists(it => it.NodeID == nodeID))
                return;
            var nodeData = node.Export();
            nodeData.NodeID = nodeID;
            exportData.Nodes.Add(nodeData);
            foreach (var item in graph.Edges)
            {
                if(item.Output == node)
                {
                    exportData.Edges.Add(ToRuntimeEdge(graph, item));
                    if(!nodes.Contains(item.Input))
                        nodes.Enqueue(item.Input);
                }
            }
            foreach (var item in graph.DataEdges)
            {
                if(item.Input == node)
                {
                    exportData.DataEdges.Add(ToRuntimeDataEdge(graph, item));
                    if(item.Output.IsDefine<IFlowDataProvider>() && !item.Output.IsDefine<IFlowInputable>())
                    {
                        exportData.DataNodeDependencies.Add(new FlowDataNodeDependency
                        {
                            NodeID = nodeID,
                            DataNodeID = graph.Nodes.IndexOf(item.Output)
                        });
                    }    
                    if (!nodes.Contains(item.Output))
                        nodes.Enqueue(item.Output);
                }
            }
        }

        public static FlowRuntimeEdge ToRuntimeEdge(FlowGraph graph, FlowEdge edge)
        {
            FlowRuntimeEdge runtimeEdge = new FlowRuntimeEdge
            {
                OutputNodeID = graph.Nodes.IndexOf(edge.Output),
                OutputIndex = edge.OutputIndex,
                InputNodeID = graph.Nodes.IndexOf(edge.Input)
            };
            return runtimeEdge;
        }

        public static FlowRuntimeDataEdge ToRuntimeDataEdge(FlowGraph graph, FlowDataEdge edge)
        {
            FlowRuntimeDataEdge runtimeEdge = new FlowRuntimeDataEdge
            {
                EdgeID = edge.EdgeID
            };
            int inputNodeIndex = graph.Nodes.IndexOf(edge.Input);
            int fieldHash = UnityEngine.Animator.StringToHash( edge.InputSlot);
            runtimeEdge.InputKey = ((ulong)inputNodeIndex << 32) | (uint)fieldHash;
            return runtimeEdge;
        }
    }
}
