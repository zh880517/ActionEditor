using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    /// <summary>
    /// 子图输出节点视图。显示所有输出端口作为输入数据端口（数据从子图内部流入此节点）。
    /// 包含"+"按钮添加新端口，每个端口有名称编辑和删除功能。
    /// </summary>
    public class SubGraphOutputNodeView : Node
    {
        public SubGraphOutputNode Node { get; private set; }
        public FlowSubGraph SubGraph { get; private set; }

        private readonly List<PortEntry> portEntries = new List<PortEntry>();

        struct PortEntry
        {
            public string GUID;
            public FlowDataPort Port;
            public TextField NameField;
        }

        public event Action OnPortListChanged;

        public SubGraphOutputNodeView(SubGraphOutputNode node, FlowSubGraph subGraph)
        {
            Node = node;
            SubGraph = subGraph;
            title = "输出";
            base.SetPosition(node.Position);

            // 隐藏默认输出容器
            outputContainer.style.display = DisplayStyle.None;

            // 添加端口按钮
            var addButton = new Button(OnAddPort);
            addButton.text = "+ 添加输出";
            addButton.style.marginTop = 4;
            addButton.style.marginBottom = 4;
            addButton.style.marginLeft = 6;
            addButton.style.marginRight = 6;
            addButton.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            addButton.style.borderTopColor = new Color(0.32f, 0.32f, 0.32f, 1f);
            addButton.style.borderRightColor = new Color(0.32f, 0.32f, 0.32f, 1f);
            addButton.style.borderBottomColor = new Color(0.32f, 0.32f, 0.32f, 1f);
            addButton.style.borderLeftColor = new Color(0.32f, 0.32f, 0.32f, 1f);
            addButton.style.borderTopWidth = 1;
            addButton.style.borderRightWidth = 1;
            addButton.style.borderBottomWidth = 1;
            addButton.style.borderLeftWidth = 1;
            extensionContainer.Add(addButton);

            RebuildPorts();
            RefreshExpandedState();

            // 禁止删除
            capabilities &= ~Capabilities.Deletable;
        }

        private void OnAddPort()
        {
            FlowGraphEditorUtil.RegisterUndo(SubGraph, "Add SubGraph Output Port");
            var newPort = new SubGraphPort
            {
                Name = $"Out{SubGraph.OutputPorts.Count}",
                GUID = Guid.NewGuid().ToString()
            };
            SubGraph.OutputPorts.Add(newPort);
            EditorUtility.SetDirty(SubGraph);
            RebuildPorts();
            OnPortListChanged?.Invoke();
        }

        private void OnRemovePort(string portGUID)
        {
            FlowGraphEditorUtil.RegisterUndo(SubGraph, "Remove SubGraph Output Port");
            int idx = SubGraph.OutputPorts.FindIndex(p => p.GUID == portGUID);
            if (idx >= 0)
            {
                // 移除相关数据连线
                SubGraph.DataEdges.RemoveAll(e => e.Input == Node && e.InputSlot == portGUID);
                Node.PortEdges.RemoveAll(e => e.PortGUID == portGUID);
                SubGraph.OutputPorts.RemoveAt(idx);
                EditorUtility.SetDirty(SubGraph);
                EditorUtility.SetDirty(Node);
            }
            RebuildPorts();
            OnPortListChanged?.Invoke();
        }

        private void OnPortNameChanged(string portGUID, string newName)
        {
            FlowGraphEditorUtil.RegisterUndo(SubGraph, "Rename SubGraph Output Port");
            for (int i = 0; i < SubGraph.OutputPorts.Count; i++)
            {
                if (SubGraph.OutputPorts[i].GUID == portGUID)
                {
                    var p = SubGraph.OutputPorts[i];
                    p.Name = newName;
                    SubGraph.OutputPorts[i] = p;
                    EditorUtility.SetDirty(SubGraph);
                    return;
                }
            }
        }

        public void RebuildPorts()
        {
            // 清除旧端口
            foreach (var entry in portEntries)
            {
                entry.Port.DisconnectAll();
                entry.Port.RemoveFromHierarchy();
            }
            portEntries.Clear();

            // 创建端口
            foreach (var port in SubGraph.OutputPorts)
            {
                // 输入数据端口（数据流入输出节点）
                var dataPort = new FlowDataPort(true, typeof(object));
                dataPort.portName = "";
                dataPort.Owner = Node;
                dataPort.FieldName = port.GUID;
                inputContainer.Add(dataPort);

                // 将删除按钮和名称编辑框嵌入端口内部（替代portName标签）
                var deleteBtn = new Button(() => OnRemovePort(port.GUID));
                deleteBtn.style.width = 16;
                deleteBtn.style.height = 16;
                deleteBtn.style.paddingTop = 0;
                deleteBtn.style.paddingBottom = 0;
                deleteBtn.style.paddingLeft = 0;
                deleteBtn.style.paddingRight = 0;
                deleteBtn.style.marginRight = 2;
                deleteBtn.style.marginLeft = 2;
                deleteBtn.style.backgroundColor = Color.clear;
                deleteBtn.style.borderTopWidth = 0;
                deleteBtn.style.borderRightWidth = 0;
                deleteBtn.style.borderBottomWidth = 0;
                deleteBtn.style.borderLeftWidth = 0;
                var deleteBtnIcon = new Image();
                deleteBtnIcon.image = EditorGUIUtility.IconContent("d_Toolbar Minus").image;
                deleteBtnIcon.style.width = 14;
                deleteBtnIcon.style.height = 14;
                deleteBtn.Add(deleteBtnIcon);
                deleteBtn.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    deleteBtn.style.backgroundColor = new Color(0.6f, 0.1f, 0.1f, 0.8f);
                    deleteBtnIcon.tintColor = Color.white;
                });
                deleteBtn.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    deleteBtn.style.backgroundColor = Color.clear;
                    deleteBtnIcon.tintColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                });
                deleteBtnIcon.tintColor = new Color(0.7f, 0.7f, 0.7f, 1f);

                var nameField = new TextField();
                nameField.value = port.Name;
                nameField.style.flexGrow = 1;
                nameField.style.minWidth = 60;
                string guid = port.GUID;
                nameField.RegisterValueChangedCallback(evt =>
                {
                    OnPortNameChanged(guid, evt.newValue);
                });

                // 输入端口布局: [connector][nameField][deleteBtn]
                dataPort.Add(nameField);
                dataPort.Add(deleteBtn);

                portEntries.Add(new PortEntry
                {
                    GUID = port.GUID,
                    Port = dataPort,
                    NameField = nameField,
                });
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        public void Refresh()
        {
            SetPosition(Node.Position);
            RebuildPorts();
        }

        public void DisconnectAll()
        {
            foreach (var entry in portEntries)
            {
                entry.Port.DisconnectAll();
            }
        }

        public FlowNodePort GetFlowPort(bool isInput, int index)
        {
            return null; // 无流程端口
        }

        public FlowNodePort GetDataPort(bool isInput, string fieldName)
        {
            if (!isInput) return null; // 只有输入端口
            foreach (var entry in portEntries)
            {
                if (entry.GUID == fieldName)
                    return entry.Port;
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
