using System.Collections.Generic;
using System.Linq;

namespace Flow
{
    public static class FlowGraphExport
    {
        public static void ExportToRuntimeData(FlowGraph graph, FlowGraphRuntimeData exportData)
        {
            exportData.Name = graph.name;
            var entryNode = graph.Nodes.FirstOrDefault(n => n.IsDefine<IFlowEntry>());
            if (entryNode == null)
            {
                throw new System.Exception("FlowGraph must have an Entry node");
            }
            Queue<FlowNode> nodes = new Queue<FlowNode>();
            if(entryNode is EntryNode)
            {
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
                nodes.Enqueue(startNode);
            }
            else
            {
                nodes.Enqueue(entryNode);
            }
            List<KeyValuePair<int, int>> nodeDepences = new List<KeyValuePair<int, int>>();
            while (nodes.Count > 0)
            {
                var first = nodes.Dequeue();
                CollectNode(graph, first, exportData, nodes, nodeDepences);
            }
            //整理数据节点依赖
            var groupDepences = nodeDepences.GroupBy(it => it.Key);
            foreach (var group in groupDepences)
            {
                FlowDataNodeDependency dependency = new FlowDataNodeDependency
                {
                    NodeID = group.Key,
                    Dependencies = group.Select(it => it.Value).Distinct().ToList()
                };
                exportData.DataNodeDependencies.Add(dependency);
            }
            for (int i = 0; i < exportData.DataNodeDependencies.Count; i++)
            {
                var dep = exportData.DataNodeDependencies[i];
                for (int j = 0; j < dep.Dependencies.Count; j++)
                {
                    var nodeID = dep.Dependencies[j];
                    foreach (var kv in nodeDepences)
                    {
                        if(kv.Key == nodeID)
                        {
                            if(!dep.Dependencies.Contains(kv.Key) && kv.Key != dep.NodeID)
                                dep.Dependencies.Add(kv.Key);
                        }
                    }
                }
                for (int j = 0; j < dep.Dependencies.Count; j++)
                {
                    dep.Dependencies[j] = exportData.Nodes.FindIndex(it=>it.NodeID == dep.Dependencies[j]);
                }
            }
        }

        public static void CollectNode(FlowGraph graph, FlowNode node, FlowGraphRuntimeData exportData, Queue<FlowNode> nodes, List<KeyValuePair<int, int>> nodeDepences)
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
                    ulong key = ((ulong)nodeID << 32) | (uint)UnityEngine.Animator.StringToHash(item.InputSlot);
                    exportData.InputKeyToEdgeID.Add(key, item.EdgeID);
                    //数据接口的依赖收集，只收集纯数据提供者
                    if (item.Output.IsDefine<IFlowDataProvider>() 
                        && !item.Output.IsDefine<IFlowInputable>() 
                        && !item.Output.IsDefine<IFlowUpdateable>())
                    {
                        nodeDepences.Add(new KeyValuePair<int, int>(nodeID, graph.Nodes.IndexOf(item.Output)));
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
    }
}
