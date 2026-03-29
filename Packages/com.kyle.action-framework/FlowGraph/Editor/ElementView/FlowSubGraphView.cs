using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    /// <summary>
    /// 子图编辑视图。左侧贴边显示Input端口列表，右侧贴边显示Output端口列表。
    /// 端口项是轻量Node（SubGraphPortItem），参与GraphView连线系统。
    /// </summary>
    public class FlowSubGraphView : FlowGraphView
    {
        public FlowSubGraph SubGraph => Graph as FlowSubGraph;
        private SubGraphPortListView inputPortList;
        private SubGraphPortListView outputPortList;
        private readonly List<SubGraphPortItem> portItems = new List<SubGraphPortItem>();

        public FlowSubGraphView(FlowSubGraph graph) : base(graph)
        {
            inputPortList = new SubGraphPortListView(graph, true);
            inputPortList.OnChanged += OnPortListChanged;
            Add(inputPortList);

            outputPortList = new SubGraphPortListView(graph, false);
            outputPortList.OnChanged += OnPortListChanged;
            Add(outputPortList);

            RebuildPortItems();
        }

        public void OnPortListChanged()
        {
            RebuildPortItems();
            Refresh();
        }

        private void RebuildPortItems()
        {
            // 移除旧的端口项
            foreach (var item in portItems)
            {
                item.DataPort.DisconnectAll();
                RemoveElement(item);
            }
            portItems.Clear();

            if (SubGraph == null)
                return;

            // 创建Input端口项（左侧）
            for (int i = 0; i < SubGraph.InputPorts.Count; i++)
            {
                var port = SubGraph.InputPorts[i];
                var item = new SubGraphPortItem(port, true, SubGraph);
                item.OnNameChanged += OnPortNameChanged;
                item.SetPosition(new Rect(-300, i * 60, 0, 0));
                portItems.Add(item);
                AddElement(item);
            }
            // 创建Output端口项（右侧）
            for (int i = 0; i < SubGraph.OutputPorts.Count; i++)
            {
                var port = SubGraph.OutputPorts[i];
                var item = new SubGraphPortItem(port, false, SubGraph);
                item.OnNameChanged += OnPortNameChanged;
                item.SetPosition(new Rect(500, i * 60, 0, 0));
                portItems.Add(item);
                AddElement(item);
            }

        }

        /// <summary>
        /// 覆写Refresh，额外处理端口项相关的连线
        /// </summary>
        public new void Refresh()
        {
            base.Refresh();
            RefreshPortEdges();
        }

        private void RefreshPortEdges()
        {
            // 查找子图DataEdges中涉及端口项的连接，创建对应的EdgeView
            foreach (var dataEdge in SubGraph.DataEdges)
            {
                // Input端口 -> 子图内部节点：Output端口项作为source
                if (dataEdge.Output == null && !string.IsNullOrEmpty(dataEdge.OutputSlot))
                {
                    var portItem = portItems.Find(p => p.IsInput && p.PortGUID == dataEdge.OutputSlot);
                    if (portItem != null && !EdgeExists(dataEdge.EdgeID))
                    {
                        var inputPort = FindNodeDataPort(dataEdge.Input, true, dataEdge.InputSlot);
                        if (inputPort != null)
                        {
                            var edgeView = new FlowEdgeView();
                            edgeView.EdgeID = dataEdge.EdgeID;
                            edgeView.output = portItem.DataPort;
                            edgeView.input = inputPort;
                            AddElement(edgeView);
                        }
                    }
                }
                // 子图内部节点 -> Output端口：Output端口项作为target
                if (dataEdge.Input == null && !string.IsNullOrEmpty(dataEdge.InputSlot))
                {
                    var portItem = portItems.Find(p => !p.IsInput && p.PortGUID == dataEdge.InputSlot);
                    if (portItem != null && !EdgeExists(dataEdge.EdgeID))
                    {
                        var outputPort = FindNodeDataPort(dataEdge.Output, false, dataEdge.OutputSlot);
                        if (outputPort != null)
                        {
                            var edgeView = new FlowEdgeView();
                            edgeView.EdgeID = dataEdge.EdgeID;
                            edgeView.output = outputPort;
                            edgeView.input = portItem.DataPort;
                            AddElement(edgeView);
                        }
                    }
                }
            }
        }

        private bool EdgeExists(ulong edgeID)
        {
            return edges.ToList().Any(e => e is FlowEdgeView fe && fe.EdgeID == edgeID);
        }

        private FlowNodePort FindNodeDataPort(FlowNode node, bool isInput, string fieldName)
        {
            if (node == null) return null;
            // 在父类的nodeViews中查找 — 需要通过反射或GetDataPort
            // 使用基类的protected方法不可行，改用GraphView的ports
            foreach (var p in ports.ToList())
            {
                if (p is FlowDataPort dp && dp.Owner == node && dp.FieldName == fieldName
                    && (isInput ? dp.direction == Direction.Input : dp.direction == Direction.Output))
                    return dp;
            }
            return null;
        }

        protected override GraphViewChange GraphViewChangedCallback(GraphViewChange changes)
        {
            // 处理端口项相关的边创建/删除
            bool needRefresh = false;

            if (changes.edgesToCreate != null)
            {
                for (int i = changes.edgesToCreate.Count - 1; i >= 0; i--)
                {
                    var edge = changes.edgesToCreate[i] as FlowEdgeView;
                    if (edge == null) continue;

                    var outputPort = edge.output as FlowDataPort;
                    var inputPort = edge.input as FlowDataPort;
                    if (outputPort == null || inputPort == null) continue;

                    // 检查是否涉及端口项
                    var outputItem = portItems.Find(p => p.DataPort == outputPort);
                    var inputItem = portItems.Find(p => p.DataPort == inputPort);

                    if (outputItem != null || inputItem != null)
                    {
                        // 端口项连线，不走普通的FlowDataPortOperateUtil
                        changes.edgesToCreate.RemoveAt(i);
                        RegisterGraphUndo(Graph, "create port edge");
                        ulong id = CreatePortEdge(outputItem, outputPort, inputItem, inputPort);
                        edge.EdgeID = id;
                        AddElement(edge);
                        edge.input.Connect(edge);
                        edge.output.Connect(edge);
                        needRefresh = true;
                    }
                }
            }

            if (changes.elementsToRemove != null)
            {
                for (int i = changes.elementsToRemove.Count - 1; i >= 0; i--)
                {
                    if (changes.elementsToRemove[i] is FlowEdgeView edgeView && edgeView.EdgeID != 0)
                    {
                        // 检查是否是端口项相关的边
                        var dataEdge = SubGraph.DataEdges.Find(e => e.EdgeID == edgeView.EdgeID);
                        if (dataEdge.Output == null || dataEdge.Input == null)
                        {
                            // 端口项边，手动处理删除
                            changes.elementsToRemove.RemoveAt(i);
                            RegisterGraphUndo(Graph, "remove port edge");
                            RemovePortEdge(dataEdge);
                            edgeView.output?.Disconnect(edgeView);
                            edgeView.input?.Disconnect(edgeView);
                            RemoveElement(edgeView);
                            needRefresh = true;
                        }
                    }
                }
            }

            // 调用基类处理普通节点的边
            var result = base.GraphViewChangedCallback(changes);

            if (needRefresh)
                Refresh();

            return result;
        }

        private ulong CreatePortEdge(SubGraphPortItem outputItem, FlowDataPort outputPort,
            SubGraphPortItem inputItem, FlowDataPort inputPort)
        {
            ulong id = (ulong)System.DateTime.Now.Ticks;
            while (SubGraph.DataEdges.Exists(e => e.EdgeID == id))
                id++;

            var edge = new FlowDataEdge { EdgeID = id };

            if (outputItem != null)
            {
                // Input端口项 -> 子图内部节点
                edge.Output = null;
                edge.OutputSlot = outputItem.PortGUID;
                edge.Input = inputPort.Owner;
                edge.InputSlot = inputPort.FieldName;
                SubGraph.SetInputPortEdgeID(outputItem.PortGUID, id);
            }
            else
            {
                // 子图内部节点 -> Output端口项
                edge.Output = outputPort.Owner;
                edge.OutputSlot = outputPort.FieldName;
                edge.Input = null;
                edge.InputSlot = inputItem.PortGUID;
                // 设置输出节点的OutputData EdgeID
                FlowDataPortOperateUtil.SetNodeSlotID(outputPort.Owner, outputPort.FieldName, id);
                SubGraph.SetOutputPortEdgeID(inputItem.PortGUID, id);
            }

            SubGraph.DataEdges.Add(edge);
            EditorUtility.SetDirty(SubGraph);
            return id;
        }

        private void RemovePortEdge(FlowDataEdge dataEdge)
        {
            if (dataEdge.Output == null)
            {
                // Input端口边
                SubGraph.SetInputPortEdgeID(dataEdge.OutputSlot, 0);
            }
            else if (dataEdge.Input == null)
            {
                // Output端口边
                FlowDataPortOperateUtil.SetNodeSlotID(dataEdge.Output, dataEdge.OutputSlot, 0);
                SubGraph.SetOutputPortEdgeID(dataEdge.InputSlot, 0);
            }
            SubGraph.DataEdges.Remove(dataEdge);
            EditorUtility.SetDirty(SubGraph);
        }

        private void OnPortNameChanged(string portGUID, string newName)
        {
            FlowGraphEditorUtil.RegisterUndo(SubGraph, "Rename SubGraph Port");
            for (int i = 0; i < SubGraph.InputPorts.Count; i++)
            {
                if (SubGraph.InputPorts[i].GUID == portGUID)
                {
                    var p = SubGraph.InputPorts[i];
                    p.Name = newName;
                    SubGraph.InputPorts[i] = p;
                    EditorUtility.SetDirty(SubGraph);
                    return;
                }
            }
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
    }
}
