using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    /// <summary>
    /// 子图端口项 — 一个轻量Node容器，仅包含一个可连线的FlowDataPort和端口名称/删除按钮。
    /// 作为GraphView的子节点参与连线系统。
    /// </summary>
    public class SubGraphPortItem : Node
    {
        public string PortGUID { get; private set; }
        public bool IsInput { get; private set; }
        public FlowDataPort DataPort { get; private set; }

        private readonly TextField nameField;

        public event Action<string, string> OnNameChanged; // portGUID, newName

        public SubGraphPortItem(SubGraphPort port, bool isInput, FlowSubGraph subGraph)
        {
            PortGUID = port.GUID;
            IsInput = isInput;
            title = "";

            // 隐藏节点标题栏
            titleContainer.style.display = DisplayStyle.None;
            // 精简样式
            style.minWidth = 120;
            style.maxWidth = 200;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            // 删除按钮
            var deleteBtn = new Button(() => OnDelete(subGraph));
            deleteBtn.text = "x";
            deleteBtn.style.width = 20;
            deleteBtn.style.height = 20;
            deleteBtn.style.marginRight = 2;
            deleteBtn.style.marginLeft = 2;
            row.Add(deleteBtn);

            // 名字编辑
            nameField = new TextField();
            nameField.value = port.Name;
            nameField.style.flexGrow = 1;
            nameField.style.minWidth = 60;
            nameField.RegisterValueChangedCallback(evt =>
            {
                OnNameChanged?.Invoke(PortGUID, evt.newValue);
            });
            row.Add(nameField);

            // 数据端口
            // Input端口 -> 输出方向（数据从此端口流出到子图内部节点）
            // Output端口 -> 输入方向（数据从子图内部节点流入此端口）
            var portDirection = isInput ? Direction.Output : Direction.Input;
            DataPort = new FlowDataPort(portDirection == Direction.Input, typeof(object));
            DataPort.portName = "";
            DataPort.Owner = null; // 无Owner节点，由FlowSubGraphView特殊处理
            DataPort.FieldName = port.GUID;

            if (isInput)
                outputContainer.Add(DataPort);
            else
                inputContainer.Add(DataPort);

            extensionContainer.Add(row);
            RefreshExpandedState();

            // 禁止拖动位置
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;
        }

        private void OnDelete(FlowSubGraph subGraph)
        {
            FlowGraphEditorUtil.RegisterUndo(subGraph, "Remove SubGraph Port");
            var ports = IsInput ? subGraph.InputPorts : subGraph.OutputPorts;
            int idx = ports.FindIndex(p => p.GUID == PortGUID);
            if (idx >= 0)
            {
                // 断开此端口相关的连线
                var edges = IsInput ? subGraph.InputPortEdges : subGraph.OutputPortEdges;
                edges.RemoveAll(e => e.PortGUID == PortGUID);
                // 移除DataEdge中使用此端口的连接
                subGraph.DataEdges.RemoveAll(e =>
                    (IsInput && e.Output == null && e.OutputSlot == PortGUID) ||
                    (!IsInput && e.Input == null && e.InputSlot == PortGUID));
                ports.RemoveAt(idx);
                EditorUtility.SetDirty(subGraph);
            }
            // FlowSubGraphView会在Refresh中重建
            var graphView = GetFirstOfType<FlowSubGraphView>();
            graphView?.OnPortListChanged();
        }

        public void SetName(string name)
        {
            nameField.SetValueWithoutNotify(name);
        }
    }

    /// <summary>
    /// 子图端口列表视图，管理一侧（Input或Output）的端口项。
    /// 不是GraphView的Node，而是一个普通VisualElement用来放置添加按钮等控制UI。
    /// 实际的端口项（SubGraphPortItem）作为Node添加到GraphView中。
    /// </summary>
    public class SubGraphPortListView : VisualElement
    {
        public readonly bool IsInput;
        public readonly FlowSubGraph SubGraph;
        public readonly List<SubGraphPortItem> PortItems = new List<SubGraphPortItem>();

        public event Action OnChanged;

        public SubGraphPortListView(FlowSubGraph subGraph, bool isInput)
        {
            SubGraph = subGraph;
            IsInput = isInput;

            // 控制面板样式：贴边，仅包含添加按钮
            style.position = Position.Absolute;
            style.top = 8;
            if (isInput)
                style.left = 8;
            else
                style.right = 8;
            pickingMode = PickingMode.Ignore;

            var addButton = new Button(OnAddPort);
            addButton.text = isInput ? "+ 输入" : "+ 输出";
            addButton.pickingMode = PickingMode.Position;
            Add(addButton);
        }

        private void OnAddPort()
        {
            FlowGraphEditorUtil.RegisterUndo(SubGraph, "Add SubGraph Port");
            var ports = IsInput ? SubGraph.InputPorts : SubGraph.OutputPorts;
            var newPort = new SubGraphPort
            {
                Name = IsInput ? $"In{ports.Count}" : $"Out{ports.Count}",
                GUID = System.Guid.NewGuid().ToString()
            };
            ports.Add(newPort);
            EditorUtility.SetDirty(SubGraph);
            OnChanged?.Invoke();
        }
    }
}
