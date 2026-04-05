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
        // 动态数据端口区域容器（放在 extensionContainer 中 subGraphField 的下方）
        private VisualElement dynamicPortsRow;
        private VisualElement dynamicInputContainer;
        private VisualElement dynamicOutputContainer;

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

            // SubGraph引用字段 - objectType 使用 TGraph（若为 TSubGraphNode<TData, TGraph> 子类）
            subGraphField = new ObjectField("子图资产");
            var tGraphType = node.GetType().GetGenericParam(typeof(TSubGraphNode<,>), 1);
            subGraphField.objectType = tGraphType ?? typeof(FlowSubGraph);
            subGraphField.value = node.SubGraph;
            subGraphField.RegisterValueChangedCallback(OnSubGraphChanged);
            extensionContainer.Add(subGraphField);

            // 动态数据端口区域：行容器（左侧输入列 + 右侧输出列），位于 subGraphField 下方
            dynamicPortsRow = new VisualElement();
            dynamicPortsRow.style.flexDirection = FlexDirection.Row;
            dynamicPortsRow.style.justifyContent = Justify.SpaceBetween;

            dynamicInputContainer = new VisualElement();
            dynamicInputContainer.style.flexDirection = FlexDirection.Column;
            dynamicInputContainer.style.marginLeft = -10f;
            dynamicInputContainer.style.paddingLeft = 10f;

            dynamicOutputContainer = new VisualElement();
            dynamicOutputContainer.style.flexDirection = FlexDirection.Column;
            dynamicOutputContainer.style.alignItems = Align.FlexEnd;
            dynamicOutputContainer.style.marginRight = -10f;
            dynamicOutputContainer.style.paddingRight = 10f;

            dynamicPortsRow.Add(dynamicInputContainer);
            dynamicPortsRow.Add(dynamicOutputContainer);
            extensionContainer.Add(dynamicPortsRow);

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
                dynamicPortsRow.style.display = DisplayStyle.None;
                RefreshPorts();
                return;
            }

            // 更新标题
            title = string.IsNullOrEmpty(Node.SubGraph.name) ? "子图" : Node.SubGraph.name;

            bool hasDynamicPorts = false;

            // 创建输入数据端口（对应SubGraph的InputPorts），类型为object时跳过
            foreach (var port in Node.SubGraph.InputPorts)
            {
                var resolvedType = ResolveInputPortType(port.GUID);
                if (resolvedType == typeof(object)) continue;
                var dataPort = new FlowDataPort(true, resolvedType);
                dataPort.portName = port.Name;
                dataPort.Owner = Node;
                dataPort.FieldName = port.GUID;
                dynamicInputContainer.Add(dataPort);
                inputDataPorts[port.GUID] = dataPort;
                ports.Add(new PortUnit { GUID = port.GUID, IsInput = true, IsFlowPort = false, Port = dataPort });
                hasDynamicPorts = true;
            }

            // 创建输出数据端口（对应SubGraph的OutputPorts），类型为object时跳过
            foreach (var port in Node.SubGraph.OutputPorts)
            {
                var resolvedType = ResolveOutputPortType(port.GUID);
                if (resolvedType == typeof(object)) continue;
                var dataPort = new FlowDataPort(false, resolvedType);
                dataPort.portName = port.Name;
                dataPort.Owner = Node;
                dataPort.FieldName = port.GUID;
                dynamicOutputContainer.Add(dataPort);
                outputDataPorts[port.GUID] = dataPort;
                ports.Add(new PortUnit { GUID = port.GUID, IsInput = false, IsFlowPort = false, Port = dataPort });
                hasDynamicPorts = true;
            }

            dynamicPortsRow.style.display = hasDynamicPorts ? DisplayStyle.Flex : DisplayStyle.None;

            RefreshPorts();
        }

        /// <summary>
        /// 通过子图内部连接推断输入端口的实际类型。
        /// InputPorts 对应 SubGraphInputNode 的输出端口，类型由其下游连接推断。
        /// </summary>
        private Type ResolveInputPortType(string portGUID)
        {
            var subGraph = Node.SubGraph;
            if (subGraph?.InputNode == null) return typeof(object);
            foreach (var edge in subGraph.DataEdges)
            {
                if (edge.Output == subGraph.InputNode && edge.OutputSlot == portGUID)
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
                }
            }
            return typeof(object);
        }

        /// <summary>
        /// 通过子图内部连接推断输出端口的实际类型。
        /// OutputPorts 对应 SubGraphOutputNode 的输入端口，类型由其上游连接推断。
        /// </summary>
        private Type ResolveOutputPortType(string portGUID)
        {
            var subGraph = Node.SubGraph;
            if (subGraph?.OutputNode == null) return typeof(object);
            foreach (var edge in subGraph.DataEdges)
            {
                if (edge.Input == subGraph.OutputNode && edge.InputSlot == portGUID)
                {
                    var outputTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(edge.Output.GetType());
                    if (outputTypeInfo != null)
                    {
                        foreach (var field in outputTypeInfo.OutputFields)
                        {
                            if (field.Name == edge.OutputSlot)
                                return field.DataType;
                        }
                    }
                }
            }
            return typeof(object);
        }

        public virtual void Refresh()
        {
            SetPosition(Node.Position);
            subGraphField.SetValueWithoutNotify(Node.SubGraph);
            RebuildDataPorts();
        }

        public virtual void DisconnectAll()
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

        public virtual FlowNodePort GetDataPort(bool isInput, string fieldName)
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
