using PropertyEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    /// <summary>
    /// TSubGraphNode&lt;TData, TGraph&gt; 的视图。
    /// 在 SubGraphNodeView 的动态端口基础上，额外渲染 TData 的静态数据端口和属性编辑字段。
    /// </summary>
    public class TSubGraphNodeView : SubGraphNodeView
    {
        private readonly FlowNodeTypeInfo typeInfo;
        private StructedPropertyElement propertyEditor;
        private readonly List<StrctedFieldElement> inputFields = new List<StrctedFieldElement>();
        private readonly List<FlowNodePort> staticPorts = new List<FlowNodePort>();

        public TSubGraphNodeView(SubGraphNode node) : base(node)
        {
            typeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(node.GetType());
            if (typeInfo?.ValueField != null)
            {
                title = typeInfo.ShowName;
                BuildStaticPorts();
                BuildValueField();
            }
            RefreshExpandedState();
        }

        private void BuildStaticPorts()
        {
            // 静态输出数据端口（来自 TData 的 IOutputData<T> 字段）
            foreach (var field in typeInfo.OutputFields)
            {
                var port = new FlowDataPort(false, field.DataType);
                port.portName = field.ShowName;
                port.Owner = Node;
                port.FieldName = field.Name;
                // 插在 Flow 输出端口之后（index 1）
                outputContainer.Insert(1, port);
                staticPorts.Add(port);
            }
        }

        private void BuildValueField()
        {
            // 用 PropertyEditor 渲染 TData Value 字段，复用框架现有反射 UI 系统
            propertyEditor = PropertyElementFactory.CreateByType(Node.GetType(), true) as StructedPropertyElement;
            propertyEditor.SetValue(Node);
            propertyEditor.SetLableWidth(50);
            propertyEditor.style.minWidth = 120;
            extensionContainer.Insert(0, propertyEditor);

            // 静态输入数据端口（来自 TData 的 [Inputable] 字段），内嵌在属性字段旁边
            foreach (var field in typeInfo.InputFields)
            {
                var element = propertyEditor.FindByPath(field.Path);
                if (element != null)
                {
                    inputFields.Add(element);
                    var port = new FlowDataPort(true, field.FieldType);
                    port.portName = element.DisplayName;
                    port.Owner = Node;
                    port.FieldName = field.Name;
                    element.Element.SetLable(null, null);
                    element.Insert(0, port);
                    staticPorts.Add(port);
                }
            }

            RefreshStaticDataPorts();
        }

        private void RefreshStaticDataPorts()
        {
            if (Node.Graph == null) return;
            foreach (var item in inputFields)
            {
                bool hasConnection = Node.Graph.DataEdges.Exists(e => e.Input == Node && e.InputSlot == item.FieldName);
                item.Element.style.display = hasConnection ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            propertyEditor?.SetValue(Node);
            RefreshStaticDataPorts();
        }

        public override void DisconnectAll()
        {
            base.DisconnectAll();
            foreach (var port in staticPorts)
                port.DisconnectAll();
        }

        public override FlowNodePort GetDataPort(bool isInput, string fieldName)
        {
            // 先查静态端口
            foreach (var port in staticPorts)
            {
                if (port is FlowDataPort dp && dp.direction == (isInput ? UnityEditor.Experimental.GraphView.Direction.Input : UnityEditor.Experimental.GraphView.Direction.Output) && dp.FieldName == fieldName)
                    return dp;
            }
            // 再查动态端口（SubGraph 端口）
            return base.GetDataPort(isInput, fieldName);
        }
    }
}
