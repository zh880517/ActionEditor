using PropertyEditor;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class FlowNodeView : Node
    {
        enum PortType
        {
            FlowInput,
            FlowOutput,
            DynamicFlowOutput,
            DataInput,
            DataOutput
        }
        struct PortUnit
        {
            public PortType Type;
            public string Name;
            public int Index;
            public FlowNodePort Port;
        }

        public FlowNode Node { get; private set; }
        private readonly List<StrctedFieldElement> inputFields = new List<StrctedFieldElement>();
        private readonly List<PortUnit> ports = new List<PortUnit>();
        private StructedPropertyElement propertyEditor;
        private FlowNodeTypeInfo nodeTypeInfo;
        private FlowDynamicOutputPort dynamicOutputPort;

        private bool isExpanded = true;

        public FlowNodeView(FlowNode node)
        {
            Node = node;
            nodeTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(node.GetType());
            title = nodeTypeInfo.ShowName;
            base.SetPosition(node.Position);
            isExpanded = node.Expanded;
            //流程输入端口
            if (nodeTypeInfo.HasInput)
            {
                var inputPort = new FlowPort(true);
                inputPort.portName = "In";
                inputPort.Owner = node;
                inputContainer.Add(inputPort);
                ports.Add(new PortUnit { Name = "In", Port = inputPort, Type = PortType.FlowInput });
            }
            //流程输出端口
            switch (nodeTypeInfo.OutputType)
            {
                case NodeOutputType.Normal:
                    {
                        var outputPort = new FlowPort(false);
                        outputPort.portName = "Out";
                        outputPort.Owner = node;
                        outputContainer.Add(outputPort);
                        ports.Add(new PortUnit { Name = "Out", Port = outputPort, Type = PortType.FlowOutput });
                    }
                    break;
                case NodeOutputType.Condition:
                    {
                        var truePort = new FlowPort(false);
                        truePort.Owner = node;
                        truePort.Index = 0;
                        truePort.portName = "True";
                        truePort.visualClass = "Port_True";
                        outputContainer.Add(truePort);
                        ports.Add(new PortUnit { Name = "True", Port = truePort, Type = PortType.FlowOutput, Index = 0 });
                        var falsePort = new FlowPort(false);
                        falsePort.Owner = node;
                        falsePort.Index = 1;
                        falsePort.portName = "False";
                        falsePort.visualClass = "Port_False";
                        outputContainer.Add(falsePort);
                        ports.Add(new PortUnit { Name = "False", Port = falsePort, Type = PortType.FlowOutput, Index = 0 });
                    }
                    break;
                case NodeOutputType.Dynamic:
                    {
                        dynamicOutputPort = new FlowDynamicOutputPort(node, nodeTypeInfo);
                        //outputContainer.Add(dynamicOutputPort);
                    }
                    break;
            }
            //数据输出端口
            foreach (var item in nodeTypeInfo.OutputFields)
            {
                var port = new FlowDataPort(false, item.DataType);
                port.portName = item.ShowName;
                port.Owner = node;
                port.FieldName = item.Name;
                outputContainer.Add(port);
                ports.Add(new PortUnit { Name = item.Name, Port = port, Type = PortType.DataOutput });
            }

            //属性编辑器
            propertyEditor = PropertyElementFactory.CreateByType(node.GetType(), true) as StructedPropertyElement;
            propertyEditor.SetValue(node);
            propertyEditor.SetLableWidth(50);
            propertyEditor.style.minWidth = 120;
            extensionContainer.Add(propertyEditor);

            //数据输入端口
            foreach (var item in nodeTypeInfo.InputFields)
            {
                var element = propertyEditor.FindByPath(item.Path);
                if (element != null)
                {
                    inputFields.Add(element);
                    var port = new FlowDataPort(true, item.FieldType);
                    port.portName = element.DisplayName;
                    port.Owner = node;
                    port.FieldName = item.Name;
                    element.Element.SetLable(null, null);
                    element.Insert(0, port);
                    ports.Add(new PortUnit { Name = item.Name, Port = port, Type = PortType.DataInput });
                }
            }
            RefreshDataPorts();
            if (dynamicOutputPort != null)
            {
                extensionContainer.Add(dynamicOutputPort);
            }
            RefreshExpandedState();
        }

        public void Refresh()
        {
            SetPosition(Node.Position);
            expanded = Node.Expanded;
            propertyEditor.SetValue(Node);
            dynamicOutputPort?.Refresh();
            RefreshDataPorts();
        }

        public void RefreshDataPorts()
        {
            foreach (var item in inputFields)
            {
                bool hasConnection = Node.Graph.DataEdges.Exists(it=>it.Input == Node && it.InputSlot == item.FieldName);
                item.Element.style.display = hasConnection ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public void DisconnectAll()
        {
            foreach (var item in ports)
            {
                item.Port.DisconnectAll();
            }
            dynamicOutputPort?.DisconnectAll();
        }

        public FlowNodePort GetFlowPort(bool isInput, int index)
        {
            foreach (var item in ports)
            {
                if (item.Type == (isInput ? PortType.FlowInput : PortType.FlowOutput))
                {
                    if(index == item.Index)
                        return item.Port;
                }
            }
            if(!isInput && dynamicOutputPort != null)
            {
                return dynamicOutputPort.GetPort(index);
            }
            return null;
        }

        public FlowNodePort GetDataPort(bool isInput, string fieldName)
        {
            foreach (var item in ports)
            {
                if (item.Type == (isInput ? PortType.DataInput : PortType.DataOutput) && item.Name == fieldName)
                {
                    return item.Port;
                }
            }
            return null;
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

        protected override void ToggleCollapse()
        {
            isExpanded = !isExpanded;
            //TODO:折叠处理，原有的折叠处理有bug，重新实现
            //1、隐藏所有没有进行连接的输入输出端口
            //2、隐藏属性编辑字段（如果是已经连接的数据端口则不隐藏）
            //3、如果没有动态输出端口被连接则隐藏整个动态输出端口
        }
    }
}
