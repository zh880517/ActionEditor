using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace Flow.EditorView
{
    public class FlowGraphView : GraphView
    {
        public FlowGraphEditorWindow EditorWindow { get; set; }
        public FlowGraph Graph { get; private set; }
        public Vector2 MouseLocalPosition { get; private set; }
        private readonly List<FlowNodeView> nodeViews = new List<FlowNodeView>();
        private readonly FlowTypeSelectWindow flowTypeSelect;
        public FlowGraphView(FlowGraph graph)
        {
            Graph = graph;
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            Insert(0, new GridBackground());
            SetupZoom(0.1f, 5f);
            flowTypeSelect = FlowTypeSelectProvider.GetTypeSelectWindow(graph);

            contentViewContainer.style.translate = graph.Position;
            contentViewContainer.style.scale = graph.Scale;

            graphViewChanged = GraphViewChangedCallback;
            viewTransformChanged = ViewTransformChangedCallback;
            unserializeAndPaste = OnUnserializeAndPaste;
            serializeGraphElements = OnSerializeGraphElements;
            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            nodeCreationRequest = (c) =>
            {
                flowTypeSelect.Current = this;
                if (EditorWindow)
                {
                    var root = EditorWindow.rootVisualElement;
                    var worldMousePosition = root.ChangeCoordinatesTo(root, c.screenMousePosition - EditorWindow.position.position);
                    flowTypeSelect.MousePosition = contentViewContainer.WorldToLocal(worldMousePosition);  
                }
                SearchWindow.Open(new SearchWindowContext(c.screenMousePosition), flowTypeSelect);
            };
            RegisterCallback<DynamicOuputPortCreateEvent>(OnDynamicOuputPortCreateEvent);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return
                ports
                    .ToList()
                    .Where(endPort => endPort.direction != startPort.direction && endPort.node != startPort.node)
                    .ToList();
        }

        public void Refresh()
        {
            //移除多余的节点
            for (int i = nodeViews.Count - 1; i >= 0; i--)
            {
                var nodeView = nodeViews[i];
                if (!Graph.Nodes.Contains(nodeView.Node))
                {
                    nodeView.DisconnectAll();
                    nodeViews.RemoveAt(i);
                    RemoveElement(nodeView);
                }
                else
                {
                    nodeView.Refresh();
                }
            }
            //添加新的节点
            foreach (var node in Graph.Nodes)
            {
                if (!nodeViews.Any(n => n.Node == node))
                {
                    var nodeView = new FlowNodeView(node);
                    nodeViews.Add(nodeView);
                    AddElement(nodeView);
                }
            }
            //移除多余的连线
            var edgeList = edges.ToList();
            HashSet<ulong> connectedDataEdges = new HashSet<ulong>();
            HashSet<int> connectedFlowEdges = new HashSet<int>();
            for (int i = 0; i < edgeList.Count; i++)
            {
                var edge = edgeList[i] as FlowEdgeView;
                if(edge == null)
                {
                    edgeList.RemoveAt(i);
                    i--;
                    continue;
                }
                if(edge.EdgeID != 0)
                {
                    if(!Graph.DataEdges.Exists(it=>it.EdgeID == edge.EdgeID))
                    {
                        RemoveEdge(edge);
                        edgeList.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        connectedDataEdges.Add(edge.EdgeID);
                    }
                }
                else
                {
                    var input = edge.input as FlowPort;
                    var output = edge.output as FlowPort;
                    if(input == null || output == null)
                    {
                        RemoveEdge(edge);
                        edgeList.RemoveAt(i);
                        i--;
                    }
                    int index = Graph.Edges.FindIndex(it => it.Input == input.Owner && it.OutputIndex == output.Index && it.Output == output.Owner);
                    if (index < 0)
                    {
                        RemoveEdge(edge);
                        edgeList.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        connectedFlowEdges.Add(index);
                    }
                }

            }

            //添加新的连线
            foreach (var item in Graph.DataEdges)
            {
                if(!connectedDataEdges.Contains(item.EdgeID))
                {
                    var outputNodeView = nodeViews.Find(n => n.Node == item.Output);
                    var inputNodeView = nodeViews.Find(n => n.Node == item.Input);
                    var outputPort = outputNodeView?.GetDataPort(false, item.OutputSlot);
                    var inputPort = inputNodeView?.GetDataPort(true, item.InputSlot);
                    if (outputPort != null && inputPort != null)
                    {
                        var edgeView = new FlowEdgeView();
                        edgeView.EdgeID = item.EdgeID;
                        edgeView.input = inputPort;
                        edgeView.output = outputPort;
                        AddElement(edgeView);
                    }
                }
            }
            foreach (var item in Graph.Edges)
            {
                if (!connectedFlowEdges.Contains(Graph.Edges.IndexOf(item)))
                {
                    var outputNodeView = nodeViews.Find(n => n.Node == item.Output);
                    var inputNodeView = nodeViews.Find(n => n.Node == item.Input);
                    var outputPort = outputNodeView?.GetFlowPort(false, item.OutputIndex);
                    var inputPort = inputNodeView?.GetFlowPort(true, 0);
                    if (outputPort != null && inputPort != null)
                    {
                        var edgeView = new FlowEdgeView();
                        edgeView.EdgeID = 0;
                        edgeView.input = inputPort;
                        edgeView.output = outputPort;
                        AddElement(edgeView);
                    }
                }
            }
        }

        private void RemoveEdge(FlowEdgeView edge)
        {
            edge.output?.Disconnect(edge);
            edge.input?.Disconnect(edge);
            RemoveElement(edge);
        }

        private void ViewTransformChangedCallback(GraphView view)
        {
            Graph.Position = contentViewContainer.resolvedStyle.translate;
            Graph.Scale = contentViewContainer.resolvedStyle.scale.value;
        }

        private void OnDynamicOuputPortCreateEvent(DynamicOuputPortCreateEvent evt)
        {
            foreach (var item in Graph.Edges)
            {
                if (item.Output == evt.Node && item.OutputIndex >= evt.Index)
                {
                    var inputNodeView = nodeViews.Find(n => n.Node == item.Input);
                    var inputPort = inputNodeView?.GetFlowPort(true, 0);
                    if(inputPort != null)
                    {
                        var edgeView = new FlowEdgeView();
                        edgeView.EdgeID = 0;
                        edgeView.input = inputPort;
                        edgeView.output = evt.Port;
                        AddElement(edgeView);
                    }
                    break;
                }
            }
        }

        protected virtual GraphViewChange GraphViewChangedCallback(GraphViewChange changes)
        {
            bool needRefresh = false;
            if (changes.elementsToRemove != null)
            {
                changes.elementsToRemove.RemoveAll(it =>
                {
                    if (it is FlowNodeView nodeView)
                    {
                        return !Graph.CheckDelete(nodeView.Node);
                    }
                    return false;
                });
                if (changes.elementsToRemove.Count > 0)
                {
                    FlowGraphEditorUtil.RegisterUndo(Graph, "remove element");
                    foreach (var ele in changes.elementsToRemove)
                    {
                        switch (ele)
                        {
                            case null:
                                break;
                            case FlowEdgeView edge:
                                if(edge.EdgeID != 0)
                                {
                                    var input = edge.input as FlowDataPort;
                                    FlowDataPortOperateUtil.DisconnectDataPort(input.Owner, input.FieldName);
                                }
                                else
                                {
                                    var output = edge.output as FlowPort;
                                    FlowPortOperateUtil.DisconnectFlowPort(output.Owner, output.Index);
                                }
                                break;
                            case FlowNodeView nodeView:
                                FlowPortOperateUtil.OnNodeRemove(nodeView.Node);
                                FlowDataPortOperateUtil.OnNodeRemove(nodeView.Node);
                                OnNodeDelete(nodeView.Node);
                                nodeView.DisconnectAll();
                                Graph.Nodes.Remove(nodeView.Node);
                                nodeViews.Remove(nodeView);
                                Undo.DestroyObjectImmediate(nodeView.Node);
                                break;
                        }
                    }
                    needRefresh = true;
                }
            }
            if (changes.edgesToCreate != null)
            {
                FlowGraphEditorUtil.RegisterUndo(Graph, "create edge");
                foreach (var edge in changes.edgesToCreate)
                {
                    var flowEdge = edge as FlowEdgeView;
                    var input = flowEdge.input as FlowNodePort;
                    var output = flowEdge.output as FlowNodePort;
                    if (input.IsFlowPort)
                    {
                        FlowPortOperateUtil.ConnectFlowPort(output.Owner, (output as FlowPort).Index , input.Owner);
                    }
                    else
                    {
                        var dataInput = input as FlowDataPort;
                        var dataOutput = output as FlowDataPort;
                        ulong id = FlowDataPortOperateUtil.ConnectDataPort(output.Owner, dataOutput.FieldName, input.Owner, dataInput.FieldName);
                        flowEdge.EdgeID = id;
                    }
                }
                needRefresh = true;
            }
            if (needRefresh)
            {
                Refresh();
            }
            return changes;
        }

        protected void OnNodeDelete(FlowNode node)
        {
            var removes = edges.Select(it => it as FlowEdgeView)
                .Where(it => it != null)
                .Where(it =>
                {
                    if (it.input is FlowNodePort input && input.Owner == node)
                        return true;
                    if (it.output is FlowNodePort output && output.Owner == node)
                        return true;
                    return false;
                })
                .ToArray();
            foreach (var edge in removes)
            {
                RemoveElement(edge);
            }
        }


        private void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            MouseLocalPosition = evt.localMousePosition;
        }

        protected virtual void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (evt.commandName == "Copy" && selection.Count() > 0)
            {
                evt.StopPropagation();
            }
            else if (evt.commandName == "Paste")
            {
                evt.StopPropagation();
            }
            else if (evt.commandName == "SelectAll")
            {
                ClearSelection();
                foreach (var nodeView in nodeViews)
                {
                    AddToSelection(nodeView);
                }
                evt.StopPropagation();
            }
            else if(evt.commandName == "DeselectAll")
            {
                ClearSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == "SoftDelete")
            {
                DeleteSelection();
                evt.StopPropagation();
            }
            else
            {
                evt.StopPropagation();
            }
        }

        protected void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (selection.Exists(it => it is Node))
            {
                if (evt.commandName == "Copy" 
                    || evt.commandName == "DeselectAll"
                    || evt.commandName == "Duplicate")
                {
                    evt.StopPropagation();
                    return;
                }
            }
            if (evt.commandName == "Paste" && !string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
            {
                evt.StopPropagation();
            }
            else if (evt.commandName == "SoftDelete" && selection.Count() > 0)
            {
                evt.StopPropagation();
            }
        }
        protected virtual void OnUnserializeAndPaste(string operationName, string data)
        {
            var copyData = FlowGraphClipboard.GetCopyData(Graph);
            if (operationName == "Duplicate")
            {
                Paste(copyData, new Vector2(50, 50));
            }
            else
            {
                Paste(copyData, Vector2.zero);
            }
        }
        protected virtual string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var copyData = ToCopyData(elements);
            if(copyData.Nodes.Count == 0)
            {
                return null;
            }
            FlowGraphClipboard.SetCopy(copyData);
            return System.DateTime.Now.ToString();
        }

        public void OnNodeCreate(System.Type type, Vector2 localPosition)
        {
            FlowGraphEditorUtil.RegisterUndo(Graph, "create node");
            FlowNode node = FlowGraphEditorUtil.CreateNode(Graph, type, localPosition);
            Undo.RegisterCreatedObjectUndo(node, "create node");
            var nodeView = new FlowNodeView(node);
            nodeViews.Add(nodeView);
            AddElement(nodeView);
        }

        public FlowGraphCopyData ToCopyData(IEnumerable<GraphElement> elements)
        {
            FlowGraphCopyData data = new FlowGraphCopyData
            {
                GraphScript = MonoScript.FromScriptableObject(Graph)
            };
            Dictionary<FlowNode, string> nodeGUIDs = new Dictionary<FlowNode, string>();
            foreach (var e in elements)
            {
                if (e is FlowNodeView nodeView)
                {
                    var node = nodeView.Node;
                    if (node .IsDefine<IFlowEntry>())
                        continue;
                    var nodeData = new FlowNodeCopy
                    {
                        GUID = System.Guid.NewGuid().ToString(),
                        NodeScript = MonoScript.FromScriptableObject(node),
                        Position = node.Position.position - MouseLocalPosition,
                        JsonData = JsonUtility.ToJson(nodeView.Node)
                    };
                    nodeGUIDs[node] = nodeData.GUID;
                    data.Nodes.Add(nodeData);
                }
            }
            foreach (var item in Graph.Edges)
            {
                if (!nodeGUIDs.TryGetValue(item.Output, out string outputGUID))
                    continue;
                if (!nodeGUIDs.TryGetValue(item.Input, out string inputGUID))
                    continue;
                var edgeData = new FlowEdgeCopy
                {
                    OutputNodeGUID = outputGUID,
                    OutputPortIndex = item.OutputIndex,
                    InputNodeGUID = inputGUID
                };
                data.Edges.Add(edgeData);
            }
            foreach (var item in Graph.DataEdges)
            {
                if (!nodeGUIDs.TryGetValue(item.Output, out string outputGUID))
                    continue;
                if (!nodeGUIDs.TryGetValue(item.Input, out string inputGUID))
                    continue;
                var edgeData = new FlowDataEdgeCopy
                {
                    OutputNodeGUID = outputGUID,
                    OutputPortName = item.OutputSlot,
                    InputNodeGUID = inputGUID,
                    InputPortName = item.InputSlot,
                };
                data.DataEdges.Add(edgeData);
            }
            return data;
        }

        public void Paste(FlowGraphCopyData copyData, Vector2 offset)
        {
            if (copyData == null || copyData.Nodes.Count == 0)
                return;
            FlowGraphEditorUtil.RegisterUndo(Graph, "paste node");
            Dictionary<string, FlowNode> nodeDict = new Dictionary<string, FlowNode>();
            foreach (var nodeData in copyData.Nodes)
            {
                Vector2 position = MouseLocalPosition + (nodeData.Position + offset);
                var node = FlowGraphEditorUtil.CreateNode(Graph, nodeData.NodeScript.GetClass(), position);
                JsonUtility.FromJsonOverwrite(nodeData.JsonData, node);
                node.Position.position = position;
                Undo.RegisterCreatedObjectUndo(node, "paste node");
                nodeDict[nodeData.GUID] = node;
                var nodeView = new FlowNodeView(node);
                nodeViews.Add(nodeView);
                AddElement(nodeView);
            }
            foreach (var edgeData in copyData.Edges)
            {
                if (!nodeDict.TryGetValue(edgeData.OutputNodeGUID, out FlowNode output))
                    continue;
                if (!nodeDict.TryGetValue(edgeData.InputNodeGUID, out FlowNode input))
                    continue;
                FlowPortOperateUtil.ConnectFlowPort(output, edgeData.OutputPortIndex, input);
            }
            foreach (var edgeData in copyData.DataEdges)
            {
                if (!nodeDict.TryGetValue(edgeData.OutputNodeGUID, out FlowNode output))
                    continue;
                if (!nodeDict.TryGetValue(edgeData.InputNodeGUID, out FlowNode input))
                    continue;
                FlowDataPortOperateUtil.ConnectDataPort(output, edgeData.OutputPortName, input, edgeData.InputPortName);
            }

            Refresh();
        }
    }
}