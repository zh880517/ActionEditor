using PropertyEditor;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UI;
using UnityEngine;

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

        public FlowNodeView(FlowNode node)
        {
            Node = node;
            nodeTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(node.GetType());
            title = nodeTypeInfo.ShowName;
            base.SetPosition(node.Position);
            expanded = node.Expanded;

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
                        outputContainer.Add(dynamicOutputPort);
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
            propertyEditor.SetLableWidth(60);
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

            RefreshExpandedState();
        }

        public void Refresh()
        {
            SetPosition(Node.Position);
            expanded = Node.Expanded;
            propertyEditor.SetValue(Node);
            dynamicOutputPort?.Refresh();
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
    }
}
