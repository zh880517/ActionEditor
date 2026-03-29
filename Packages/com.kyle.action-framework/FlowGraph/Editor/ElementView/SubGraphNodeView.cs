using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    /// <summary>
    /// SubGraphNode在父图编辑器中的视图。
    /// 动态根据SubGraph的InputPorts/OutputPorts生成数据端口。
    /// 端口类型未连接时为typeof(object)，连接后从对端推断。
    /// </summary>
    public class SubGraphNodeView : Node
    {
        public SubGraphNode Node { get; private set; }

        private readonly List<PortUnit> ports = new List<PortUnit>();

        // portGUID -> FlowDataPort
        private readonly Dictionary<string, FlowDataPort> inputDataPorts = new Dictionary<string, FlowDataPort>();
        private readonly Dictionary<string, FlowDataPort> outputDataPorts = new Dictionary<string, FlowDataPort>();

        private ObjectField subGraphField;

        struct PortUnit
        {
            public string GUID;
            public bool IsInput;
            public bool IsFlowPort;
            public FlowNodePort Port;
        }

        public SubGraphNodeView(SubGraphNode node)
        {
            Node = node;
            title = "子图";
            base.SetPosition(node.Position);

            // Flow输入端口
            var inputPort = new FlowPort(true);
            inputPort.portName = "In";
            inputPort.Owner = node;
            inputContainer.Add(inputPort);
            ports.Add(new PortUnit { IsInput = true, IsFlowPort = true, Port = inputPort });

            // Flow输出端口
            var outputPort = new FlowPort(false);
            outputPort.portName = "Out";
            outputPort.Owner = node;
            outputContainer.Add(outputPort);
            ports.Add(new PortUnit { IsInput = false, IsFlowPort = true, Port = outputPort });

            // SubGraph引用字段
            subGraphField = new ObjectField("子图资产");
            subGraphField.objectType = typeof(FlowSubGraph);
            subGraphField.value = node.SubGraph;
            subGraphField.RegisterValueChangedCallback(OnSubGraphChanged);
            extensionContainer.Add(subGraphField);

            // 根据SubGraph生成动态数据端口
            RebuildDataPorts();

            RefreshExpandedState();
        }

        private void OnSubGraphChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            FlowGraphEditorUtil.RegisterUndo(Node, "Change SubGraph Reference");
            Node.SubGraph = evt.newValue as FlowSubGraph;
            RebuildDataPorts();
        }

        private void RebuildDataPorts()
        {
            // 清除旧的动态数据端口
            foreach (var kv in inputDataPorts)
            {
                kv.Value.DisconnectAll();
                kv.Value.RemoveFromHierarchy();
            }
            foreach (var kv in outputDataPorts)
            {
                kv.Value.DisconnectAll();
                kv.Value.RemoveFromHierarchy();
            }
            inputDataPorts.Clear();
            outputDataPorts.Clear();
            ports.RemoveAll(p => !p.IsFlowPort);

            if (Node.SubGraph == null)
            {
                RefreshPorts();
                return;
            }

            // 更新标题
            title = string.IsNullOrEmpty(Node.SubGraph.name) ? "子图" : Node.SubGraph.name;

            // 创建输入数据端口（对应SubGraph的InputPorts）
            foreach (var port in Node.SubGraph.InputPorts)
            {
                var resolvedType = ResolveInputPortType(port.GUID);
                var dataPort = new FlowDataPort(true, resolvedType);
                dataPort.portName = port.Name;
                dataPort.Owner = Node;
                dataPort.FieldName = port.GUID; // 使用GUID作为FieldName
                inputContainer.Add(dataPort);
                inputDataPorts[port.GUID] = dataPort;
                ports.Add(new PortUnit { GUID = port.GUID, IsInput = true, IsFlowPort = false, Port = dataPort });
            }

            // 创建输出数据端口（对应SubGraph的OutputPorts）
            foreach (var port in Node.SubGraph.OutputPorts)
            {
                var resolvedType = ResolveOutputPortType(port.GUID);
                var dataPort = new FlowDataPort(false, resolvedType);
                dataPort.portName = port.Name;
                dataPort.Owner = Node;
                dataPort.FieldName = port.GUID;
                outputContainer.Add(dataPort);
                outputDataPorts[port.GUID] = dataPort;
                ports.Add(new PortUnit { GUID = port.GUID, IsInput = false, IsFlowPort = false, Port = dataPort });
            }

            RefreshPorts();
        }

        /// <summary>
        /// 根据已有连接推断输入端口类型，未连接时返回typeof(object)
        /// </summary>
        private Type ResolveInputPortType(string portGUID)
        {
            if (Node.Graph == null) return typeof(object);
            foreach (var edge in Node.Graph.DataEdges)
            {
                if (edge.Input == Node && edge.InputSlot == portGUID)
                {
                    // 找到连接的输出端口，获取其类型
                    var outputTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(edge.Output.GetType());
                    if (outputTypeInfo != null)
                    {
                        foreach (var field in outputTypeInfo.OutputFields)
                        {
                            if (field.Name == edge.OutputSlot)
                                return field.DataType;
                        }
                    }
                    // 如果对端也是SubGraphNode，检查其端口类型
                    if (edge.Output is SubGraphNode otherSub && otherSub.SubGraph != null)
                    {
                        return typeof(object); // 递归情况暂不处理
                    }
                }
            }
            return typeof(object);
        }

        private Type ResolveOutputPortType(string portGUID)
        {
            if (Node.Graph == null) return typeof(object);
            foreach (var edge in Node.Graph.DataEdges)
            {
                if (edge.Output == Node && edge.OutputSlot == portGUID)
                {
                    var inputTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(edge.Input.GetType());
                    if (inputTypeInfo != null)
                    {
                        foreach (var field in inputTypeInfo.InputFields)
                        {
                            if (field.Name == edge.InputSlot)
                                return field.FieldType;
                        }
                    }
                    if (edge.Input is SubGraphNode)
                    {
                        return typeof(object);
                    }
                }
            }
            return typeof(object);
        }

        public void Refresh()
        {
            SetPosition(Node.Position);
            subGraphField.SetValueWithoutNotify(Node.SubGraph);
            RebuildDataPorts();
        }

        public void DisconnectAll()
        {
            foreach (var unit in ports)
            {
                unit.Port.DisconnectAll();
            }
        }

        public FlowNodePort GetFlowPort(bool isInput, int index)
        {
            foreach (var unit in ports)
            {
                if (unit.IsFlowPort && unit.IsInput == isInput)
                    return unit.Port;
            }
            return null;
        }

        public FlowNodePort GetDataPort(bool isInput, string fieldName)
        {
            if (isInput)
            {
                inputDataPorts.TryGetValue(fieldName, out var port);
                return port;
            }
            else
            {
                outputDataPorts.TryGetValue(fieldName, out var port);
                return port;
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (Node != null)
            {
                FlowGraphEditorUtil.RegisterUndo(Node, "Move Node");
                Node.Position = newPos;
            }
        }
    }
}
