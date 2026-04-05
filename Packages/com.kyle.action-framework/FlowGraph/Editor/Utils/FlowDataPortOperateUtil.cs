using System;
using System.Linq;

namespace Flow.EditorView
{
    public static class FlowDataPortOperateUtil
    {
        public static void OnNodeRemove(FlowNode node)
        {
            node.Graph.DataEdges.RemoveAll(e => e.Input == node || e.Output == node);
        }
        public static ulong ConnectDataPort(FlowNode output, string ouputFieldName, FlowNode input, string inputFieldName)
        {
            var graph = output.Graph;
            for (int i = 0; i < graph.DataEdges.Count; i++)
            {
                var e = graph.DataEdges[i];
                if (e.Input == input && e.InputSlot == inputFieldName)
                {
                    graph.DataEdges.RemoveAt(i);
                    i--;
                    continue;
                }
            }
            //生成唯一ID,使用Ticks保证唯一性，但如果在同一Tick内生成了多个连接，则需要自增
            ulong id = (ulong)System.DateTime.Now.Ticks;
            while (graph.DataEdges.Exists(it=>it.EdgeID == id))
            {
                id++;
            }
            var edge = new FlowDataEdge
            {
                Output = output,
                OutputSlot = ouputFieldName,
                Input = input,
                InputSlot = inputFieldName,
                EdgeID = id,
            };
            SetNodeSlotID(output, ouputFieldName, edge.EdgeID);
            SetInputNodeSlotID(input, inputFieldName, edge.EdgeID);
            graph.DataEdges.Add(edge);
            return edge.EdgeID;
        }

        public static ulong ConnectDataPortWithUndo(FlowNode output, string ouputFieldName, FlowNode input, string inputFieldName, string undoName)
        {
            if (output == null || input == null)
                return 0;
            FlowGraphEditorUtil.RegisterUndo(output, undoName, true);
            return ConnectDataPort(output, ouputFieldName, input, inputFieldName);
        }

        public static void DisconnectDataPort(FlowNode input, string inputFieldName)
        {
            var graph = input.Graph;
            for (int i = 0; i < graph.DataEdges.Count; i++)
            {
                var e = graph.DataEdges[i];
                if (e.Input == input && e.InputSlot == inputFieldName)
                {
                    graph.DataEdges.RemoveAt(i);
                    i--;
                    SetNodeSlotID(e.Output, e.OutputSlot, 0);
                    SetInputNodeSlotID(input, inputFieldName, 0);
                    continue;
                }
            }
        }

        /// <summary>
        /// 设置输入侧节点的EdgeID（对SubGraphNode和SubGraphOutputNode生效）
        /// </summary>
        private static void SetInputNodeSlotID(FlowNode input, string fieldName, ulong id)
        {
            if (input is SubGraphNode subNode)
            {
                subNode.SetInputEdgeID(fieldName, id);
            }
            else if (input is SubGraphOutputNode outputNode)
            {
                outputNode.SetPortEdgeID(fieldName, id);
            }
        }

        public static void SetNodeSlotID(FlowNode output, string name, ulong id)
        {
            // SubGraphNode的端口使用GUID作为name，存储在专用列表中
            if (output is SubGraphNode subNode)
            {
                subNode.SetOutputEdgeID(name, id);
                return;
            }
            // SubGraphInputNode的端口使用GUID作为name
            if (output is SubGraphInputNode inputNode)
            {
                inputNode.SetPortEdgeID(name, id);
                return;
            }
            var typeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(output.GetType());
            var field = typeInfo.OutputFields.FirstOrDefault(f => f.Name == name);
            if (field != null)
            {
                if(field.Parent == null)
                {
                    var value = field.Field.GetValue(output) as IOutputData;
                    value.EdgeID = id;
                    field.Field.SetValue(output, value);
                }
                else
                {
                    var parentValue = field.Parent.GetValue(output);
                    var value = field.Field.GetValue(parentValue) as IOutputData;
                    value.EdgeID = id;
                    field.Field.SetValue(parentValue, value);
                    field.Parent.SetValue(output, parentValue);
                }
            }
        }

        public static void DisconnectDataPortWithUndo(FlowNode input, string inputFieldName, string undoName)
        {
            if (input == null)
                return;
            FlowGraphEditorUtil.RegisterUndo(input, undoName, true);
            DisconnectDataPort(input, inputFieldName);
        }

        //修复数据端口连接，删除无效连接，修复重命名的端口

        public static void RepairDataPorts(FlowGraph graph)
        {
            var nodes = graph.Nodes;
            var edges = graph.DataEdges;
            for (int i = edges.Count - 1; i >= 0; i--)
            {
                var e = edges[i];
                if (e.Input == null || e.Output == null
                    || !nodes.Contains(e.Input) || !nodes.Contains(e.Output))
                {
                    if(e.Output)
                    {
                        SetNodeSlotID(e.Output, e.OutputSlot, 0);
                    }
                    edges.RemoveAt(i);
                    continue;
                }

                // 验证端口 slot 有效性并获取类型
                var outputType = ResolveOutputSlotType(e.Output, e.OutputSlot);
                if (outputType == null)
                {
                    if (e.Output)
                        SetNodeSlotID(e.Output, e.OutputSlot, 0);
                    edges.RemoveAt(i);
                    continue;
                }

                var inputType = ResolveInputSlotType(e.Input, e.InputSlot);
                if (inputType == null)
                {
                    if (e.Output)
                        SetNodeSlotID(e.Output, e.OutputSlot, 0);
                    edges.RemoveAt(i);
                    continue;
                }

                // 类型匹配检查（两端均为具体类型时才检查）
                if (outputType != typeof(object) && inputType != typeof(object))
                {
                    if (!inputType.IsAssignableFrom(outputType))
                    {
                        if (e.Output)
                            SetNodeSlotID(e.Output, e.OutputSlot, 0);
                        edges.RemoveAt(i);
                        continue;
                    }
                }

                // 非 SubGraph 节点：处理字段重命名修复
                if (!(e.Output is SubGraphNode || e.Output is SubGraphInputNode)
                    && !(e.Input is SubGraphNode || e.Input is SubGraphOutputNode))
                {
                    var inputTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(e.Input.GetType());
                    var inputField = inputTypeInfo.InputFields.FirstOrDefault(f => f.Name == e.InputSlot || f.OldName == e.InputSlot);
                    var outputTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(e.Output.GetType());
                    var outField = outputTypeInfo.OutputFields.FirstOrDefault(f => f.Name == e.OutputSlot || f.OldName == e.OutputSlot);
                    if (inputField != null && inputField.OldName == e.InputSlot)
                        e.InputSlot = inputField.Name;
                    if (outField != null && outField.OldName == e.OutputSlot)
                        e.OutputSlot = outField.Name;
                }
            }
        }

        /// <summary>
        /// 验证 OutputSlot 是否有效，返回对应数据类型；无效返回 null，SubGraph 动态端口返回 typeof(object)
        /// </summary>
        private static Type ResolveOutputSlotType(FlowNode output, string slot)
        {
            // SubGraphNode：输出端口 GUID 来自其引用的 SubGraph.OutputPorts
            if (output is SubGraphNode subOut)
            {
                if (subOut.SubGraph == null) return null;
                return subOut.SubGraph.OutputPorts.Exists(p => p.GUID == slot) ? typeof(object) : null;
            }
            // SubGraphInputNode：输出端口 GUID 来自所在子图的 InputPorts（数据从此流出到子图内部）
            if (output is SubGraphInputNode)
            {
                var subGraph = output.Graph as FlowSubGraph;
                if (subGraph == null) return null;
                return subGraph.InputPorts.Exists(p => p.GUID == slot) ? typeof(object) : null;
            }
            var typeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(output.GetType());
            if (typeInfo == null) return null;
            var field = typeInfo.OutputFields.FirstOrDefault(f => f.Name == slot || f.OldName == slot);
            return field?.DataType;
        }

        /// <summary>
        /// 验证 InputSlot 是否有效，返回对应数据类型；无效返回 null，SubGraph 动态端口返回 typeof(object)
        /// </summary>
        private static Type ResolveInputSlotType(FlowNode input, string slot)
        {
            // SubGraphNode：输入端口 GUID 来自其引用的 SubGraph.InputPorts
            if (input is SubGraphNode subIn)
            {
                if (subIn.SubGraph == null) return null;
                return subIn.SubGraph.InputPorts.Exists(p => p.GUID == slot) ? typeof(object) : null;
            }
            // SubGraphOutputNode：输入端口 GUID 来自所在子图的 OutputPorts（数据从子图内部流入此节点）
            if (input is SubGraphOutputNode)
            {
                var subGraph = input.Graph as FlowSubGraph;
                if (subGraph == null) return null;
                return subGraph.OutputPorts.Exists(p => p.GUID == slot) ? typeof(object) : null;
            }
            var typeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(input.GetType());
            if (typeInfo == null) return null;
            var field = typeInfo.InputFields.FirstOrDefault(f => f.Name == slot || f.OldName == slot);
            return field?.FieldType;
        }
    }
}
