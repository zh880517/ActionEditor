using System.Collections.Generic;
using System.Linq;

namespace Flow.EditorView
{
    public static class FlowPortOperateUtil
    {
        public static void OnNodeRemove(FlowNode node)
        {
            node.Graph.Edges.RemoveAll(e => e.Input == node || e.Output == node);
        }
        public static void ConnectFlowPort(FlowNode output, int index, FlowNode input)
        {
            var graph = output.Graph;
            int insertIndex = -1;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var e = graph.Edges[i];
                if (e.Output == output && e.OutputIndex == index)
                {
                    graph.Edges.RemoveAt(i);
                    i--;
                    continue;
                }
                if (e.Output == output)
                {
                    if (e.OutputIndex > index)
                    {
                        insertIndex = i;
                    }
                    else
                    {
                        insertIndex = i + 1;
                    }
                }
            }
            if (insertIndex == -1)
            {
                insertIndex = graph.Edges.Count;
            }
            var edge = new FlowEdge
            {
                Output = output,
                OutputIndex = index,
                Input = input
            };
            graph.Edges.Insert(insertIndex, edge);
        }
        public static void ConnectFlowPortWithUndo(FlowNode output, int index, FlowNode input)
        {
            if (output == null || input == null)
                return;
            FlowGraphEditorUtil.RegisterUndo(output, "connect flow port", true);
            ConnectFlowPort(output, index, input);
        }
        public static void DisconnectFlowPort(FlowNode output, int index)
        {
            var graph = output.Graph;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var e = graph.Edges[i];
                if (e.Output == output && e.OutputIndex == index)
                {
                    graph.Edges.RemoveAt(i);
                    break;
                }
            }
        }
        public static void DisconnectFlowPortWithUndo(FlowNode output, int index)
        {
            if (output == null)
                return;
            FlowGraphEditorUtil.RegisterUndo(output, "disconnect flow port", true);
            DisconnectFlowPort(output, index);
        }
        public static void DisconnectAllInputPort(FlowNode input)
        {
            var graph = input.Graph;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var e = graph.Edges[i];
                if (e.Input == input)
                {
                    graph.Edges.RemoveAt(i);
                    i--;
                }
            }
        }
        public static void DisconnectAllInputPortWithUndo(FlowNode input)
        {
            if (input == null)
                return;
            FlowGraphEditorUtil.RegisterUndo(input, "disconnect all input port", true);
            DisconnectAllInputPort(input);
        }

        public static void RemoveDynamicOutputPort(FlowNode node, int index)
        {
            var graph = node.Graph;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var e = graph.Edges[i];
                if (e.Output == node)
                {
                    if (e.OutputIndex == index)
                    {
                        graph.Edges.RemoveAt(i);
                        i--;
                    }
                    else if (e.OutputIndex > index)
                    {
                        e.OutputIndex--;
                    }
                }
            }
        }

        public static void RemoveDynamicOutputPortWithUndo(FlowNode node, int index)
        {
            if (node == null || !node.IsDefine <IFlowDynamicOutputable>())
                return;
            FlowGraphEditorUtil.RegisterUndo(node, "remove dynamic port", true);
            RemoveDynamicOutputPort(node, index);
        }

        public static void RemoveDynamicOutputPortWithUndo(FlowNode node, IEnumerable<int> indexs)
        {
            if (node == null || !node.IsDefine<IFlowDynamicOutputable>())
                return;
            FlowGraphEditorUtil.RegisterUndo(node, "remove dynamic port", true);
            var graph = node.Graph;
            indexs = indexs.OrderByDescending(i => i);
            foreach (var index in indexs)
            {
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    var e = graph.Edges[i];
                    if (e.Output == node)
                    {
                        if (e.OutputIndex == index)
                        {
                            graph.Edges.RemoveAt(i);
                            i--;
                        }
                        else if (e.OutputIndex > index)
                        {
                            e.OutputIndex--;
                        }
                    }
                }
            }
        }

        public static void DynamicOutputPortIndexChangedWithUndo(FlowNode node, int srcIndex, int dstIndex)
        {
            if (node == null || !node.IsDefine<IFlowDynamicOutputable>())
                return;
            FlowGraphEditorUtil.RegisterUndo(node, "dynamic port index changed", true);
            var graph = node.Graph;
            //先按照移除srcIndex，再插入dstIndex的逻辑处理
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var e = graph.Edges[i];
                if (e.Output == node)
                {
                    if (e.OutputIndex == srcIndex)
                    {
                        e.OutputIndex = dstIndex;
                        continue;
                    }
                    //先处理srcIndex的移除
                    if (e.OutputIndex > srcIndex)
                        e.OutputIndex--;
                    //再处理dstIndex的插入
                    if (e.OutputIndex >= dstIndex)
                        e.OutputIndex++;
                }
            }
        }

        public static void RepairFlowPorts(FlowGraph graph)
        {
            var nodes = graph.Nodes;
            var edges = graph.Edges;
            for (int i = edges.Count - 1; i >= 0; i--)
            {
                var e = edges[i];
                if (!nodes.Contains(e.Input) || !nodes.Contains(e.Output))
                {
                    edges.RemoveAt(i);
                    continue;
                }
                if(!e.Output.IsDefine<IFlowOutputable>() || !e.Input.IsDefine<IFlowInputable>())
                {
                    edges.RemoveAt(i);
                    continue;
                }

                if (e.Output.IsDefine<IFlowEntry>())
                {
                    if (e.OutputIndex != 0)
                    {
                        edges.RemoveAt(i);
                        continue;
                    }
                }
                else if (e.Output.IsDefine<IFlowConditionable>())
                {
                    if (e.OutputIndex != 0 && e.OutputIndex != 1)
                    {
                        edges.RemoveAt(i);
                        continue;
                    }
                }
                else if (e.Output.IsDefine<IFlowDynamicOutputable>())
                {
                    var ouputNodeTypeInfor = FlowNodeTypeUtil.GetNodeTypeInfo(e.Output.GetType());
                    if (ouputNodeTypeInfor == null || ouputNodeTypeInfor.OutputType != NodeOutputType.Dynamic)
                    {
                        edges.RemoveAt(i);
                        continue;
                    }
                    var ports = ouputNodeTypeInfor.DynamicPortField.GetValue(e.Output) as System.Collections.IList;
                    if (ports == null || e.OutputIndex < 0 || e.OutputIndex >= ports.Count)
                    {
                        edges.RemoveAt(i);
                        continue;
                    }
                }
                else if (e.Output.IsDefine<IFlowOutputable>())
                {
                    if (e.OutputIndex != 0)
                    {
                        edges.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    //没有输出端口的节点，不应该有输出连接
                    edges.RemoveAt(i);
                    continue;
                }
            }
        }
    }
}
