using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class CustomFlowGraphWindowAttribute : System.Attribute
    {
        public System.Type GraphType { get; private set; }
        public CustomFlowGraphWindowAttribute(System.Type graphType)
        {
            GraphType = graphType;
        }
    }

    public class FlowEditorWindow : EditorWindow
    {
        private static Dictionary<System.Type, System.Type> windowTypeCache;
        [UnityEditor.Callbacks.OnOpenAsset(0)]
        internal static bool OnGraphOpened(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is FlowGraph asset)
            {
                if(windowTypeCache == null)
                {
                    windowTypeCache = new Dictionary<System.Type, System.Type>();
                    foreach (var item in TypeCollector<FlowEditorWindow>.Types)
                    {
                        var attrs = item.GetCustomAttributes<CustomFlowGraphWindowAttribute>();
                        foreach (var attr in attrs)
                        {
                            if (attr.GraphType != null && !windowTypeCache.ContainsKey(attr.GraphType))
                            {
                                windowTypeCache.Add(attr.GraphType, item);
                            }
                        }
                    }
                }
                if(windowTypeCache.TryGetValue(asset.GetType(), out var windowType))
                {
                    var window = GetWindow(windowType) as FlowEditorWindow;
                    window.OnOpen(asset);
                }
                else
                {
                    GetWindow<FlowEditorWindow>("Flow Graph").OnOpen(asset);
                }
                return true;
            }

            return false;
        }

        [SerializeField]
        protected List<FlowGraph> openList = new List<FlowGraph>();
        [SerializeField]
        protected FlowGraph current;

        protected FlowGraphView graphView;
        private readonly Dictionary<FlowGraph, FlowGraphView> views = new Dictionary<FlowGraph, FlowGraphView>();

        protected virtual void OnOpen(FlowGraph graph)
        {
            if (graph == current)
                return;
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
                        rootVisualElement.Remove(kv.Value);
                        views.Remove(kv.Key);
                    }
                }
            }
        }

        private void CreateGUI()
        {
            if(current)
            {
                GraphViewRefresh();
            }
        }

        protected virtual void OnEnable()
        {
            if(rootVisualElement != null && current)
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
                if(graphView != view)
                    view.Refresh();
                return view;
            }
            view = CreateGraphView(graph);
            view.StretchToParentSize();
            views.Add(graph, view);
            rootVisualElement.Add(graphView);
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

        }
    }
}
