using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flow.EditorView
{

    public class FlowGraphEditorWindow : EditorWindow, IFlowEditorWindow
    {
        [UnityEditor.Callbacks.OnOpenAsset(0)]
        internal static bool OnGraphOpened(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is FlowGraph asset)
            {
                System.Type windowType = null;
                var editorContext = FlowGraphEditorContext.GetContext(asset.GetType());
                if(editorContext != null)
                {
                    windowType = editorContext.GetEditorWindowType();
                }
                if (windowType == null)
                {
                    windowType = typeof(FlowGraphEditorWindow);
                }
                var window = GetWindow(windowType) as FlowGraphEditorWindow;
                window.OnOpen(asset);
                return true;
            }

            return false;
        }

        [SerializeField]
        protected List<FlowGraph> openList = new List<FlowGraph>();
        [SerializeField]
        protected FlowGraph current;
        protected virtual VisualElement graphContainerView => rootVisualElement;
        protected FlowGraphView graphView;
        private readonly Dictionary<FlowGraph, FlowGraphView> views = new Dictionary<FlowGraph, FlowGraphView>();

        protected virtual void OnOpen(FlowGraph graph)
        {
            if (graph == current)
                return;
            Undo.RegisterCompleteObjectUndo(this, name);
            openList.Remove(graph);
            openList.Add(graph);
            current = graph;
            if(rootVisualElement != null)
            {
                GraphViewRefresh();
            }
            OnGraphChanged();

            //同时打开的图过多时，关闭最早打开的图
            if (views.Count > 20)
            {
                int minIndex = openList.Count - 15;

                foreach (var kv in views)
                {
                    int index = openList.IndexOf(kv.Key);
                    if(index <= minIndex)
                    {
                        kv.Value.RemoveFromHierarchy();
                        views.Remove(kv.Key);
                    }
                }
            }
        }

        private void CreateGUI()
        {
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            OnCreateUI();
            if (current)
            {
                GraphViewRefresh();
            }
        }

        protected virtual void OnCreateUI()
        {

        }

        protected virtual void OnEnable()
        {
            if(graphContainerView != null && current)
            {
                GraphViewRefresh();
            }
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        protected virtual void OnDestroy()
        {
            foreach (var item in openList)
            {
                if(EditorUtility.IsDirty(item))
                {
                    OnSave(item);
                    AssetDatabase.SaveAssetIfDirty(item);
                }
            }
        }

        protected virtual void OnUndoRedoPerformed()
        {
            if (graphView != null && graphView.Graph == current)
            {
                graphView.Refresh();
                return;
            }
            var prev = current;
            GraphViewRefresh();
            if (prev != current)
            {
                OnGraphChanged();
            }
        }

        protected FlowGraphView GetGraphView(FlowGraph graph)
        {
            if(views.TryGetValue(graph, out var view))
            {
                return view;
            }
            view = CreateGraphView(graph);
            view.StretchToParentSize();
            view.EditorWindow = this;
            view.EditorData = FlowGraphEditorDataCache.instance.GetEditorData(this, graph);
            views.Add(graph, view);
            graphContainerView.Add(view);
            return view;
        }

        protected virtual FlowGraphView CreateGraphView(FlowGraph graph)
        {
            return new FlowGraphView(graph);
        }

        protected void GraphViewRefresh()
        {
            var view = GetGraphView(current);
            if(view != graphView)
            {
                if(graphView != null)
                    graphView.style.display = DisplayStyle.None;
                graphView = view;
                graphView.style.display = DisplayStyle.Flex;
                graphView.Refresh();
            }
        }

        protected virtual void OnGraphChanged()
        {
        }

        protected virtual void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.S && evt.actionKey)
            {
                foreach (var item in openList)
                {
                    if (EditorUtility.IsDirty(item))
                    {
                        OnSave(item);
                        AssetDatabase.SaveAssetIfDirty(item);
                    }
                }
                evt.StopPropagation();
            }
        }

        protected virtual void OnSave(FlowGraph graph)
        {
            var editorContext = FlowGraphEditorContext.GetContext(graph.GetType());
            editorContext?.Export(graph);
        }

        public Vector2 ScreenPositionToWorldPosition(Vector2 screenPosition)
        {
            if(rootVisualElement != null)
            {
                return rootVisualElement.ChangeCoordinatesTo(rootVisualElement, screenPosition - position.position);
            }
            return screenPosition;
        }

        public Vector2 WorldPositionToScreenPosition(Vector2 worldPosition)
        {
            if (rootVisualElement != null)
            {
                return worldPosition + position.position;
            }
            return worldPosition;
        }
    }
}
