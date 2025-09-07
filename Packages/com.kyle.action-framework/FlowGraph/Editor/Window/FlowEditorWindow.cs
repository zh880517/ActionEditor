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
        private Dictionary<FlowGraph, FlowGraphView> views = new Dictionary<FlowGraph, FlowGraphView>();

        protected virtual void OnOpen(FlowGraph graph)
        {
            if(!openList.Contains(graph))
            {
                openList.Add(graph);
            }
            if(graph != current)
            {
                current = graph;
            }
            if(rootVisualElement != null)
            {
                GraphViewRefresh();
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
            GraphViewRefresh();
        }

        protected virtual FlowGraphView CreateGraphView(FlowGraph graph)
        {
            if(views.TryGetValue(graph, out var view))
            {
                if(graphView != view)
                    view.Refresh();
                return view;
            }
            view = new FlowGraphView(graph);
            view.StretchToParentSize();
            views.Add(graph, view);
            rootVisualElement.Add(graphView);
            return view;
        }

        protected void GraphViewRefresh()
        {
            var view = CreateGraphView(current);
            if(view != graphView)
            {
                if(graphView != null)
                    graphView.style.display = DisplayStyle.None;
                graphView = view;
                graphView.style.display = DisplayStyle.Flex;
            }
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
