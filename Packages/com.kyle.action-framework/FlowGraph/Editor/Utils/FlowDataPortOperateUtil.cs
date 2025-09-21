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
                    graph.Edges.RemoveAt(i);
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
                    continue;
                }
            }
        }

        public static void SetNodeSlotID(FlowNode output, string name, ulong id)
        {
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
                var inputTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(e.Input.GetType());
                var inputField = inputTypeInfo.InputFields.FirstOrDefault(f => f.Name == e.InputSlot || f.OldName == e.InputSlot);
                if (inputField == null)
                {
                    if (e.Output)
                    {
                        SetNodeSlotID(e.Output, e.OutputSlot, 0);
                    }
                    edges.RemoveAt(i);
                    continue;
                }
                var outputTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(e.Output.GetType());
                var outField = outputTypeInfo.OutputFields.FirstOrDefault(f => f.Name == e.OutputSlot || f.OldName == e.OutputSlot);
                if (outField == null || !inputField.FieldType.IsAssignableFrom(outField.DataType))
                {
                    if (e.Output)
                    {
                        SetNodeSlotID(e.Output, e.OutputSlot, 0);
                    }
                    edges.RemoveAt(i);
                    continue;
                }
                if (inputField.OldName == e.InputSlot)
                {
                    e.InputSlot = inputField.Name;
                }
                if (outField.OldName == e.OutputSlot)
                {
                    e.OutputSlot = outField.Name;
                }
            }
        }

    }
}
