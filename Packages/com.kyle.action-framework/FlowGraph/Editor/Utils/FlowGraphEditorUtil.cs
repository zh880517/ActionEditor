using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Flow.EditorView
{
    public static class FlowGraphEditorUtil
    {
        public static void RegisterUndo(FlowGraph graph, string name)
        {
            if (graph == null)
                return;
            Undo.RegisterCompleteObjectUndo(graph, name);
            EditorUtility.SetDirty(graph);
        }

        public static void RegisterUndo(FlowNode node, string name, bool includeGraph = false)
        {
            if (node == null || node.Graph == null)
                return;
            Undo.RegisterCompleteObjectUndo(node, name);
            if (includeGraph)
                Undo.RegisterCompleteObjectUndo(node.Graph, name);
            EditorUtility.SetDirty(node.Graph);
        }

        public static FlowNode CreateNode<T>(FlowGraph graph, Vector2 position) where T : FlowNode
        {
            return CreateNode(graph, typeof(T), position);
        }

        public static FlowNode CreateNode(FlowGraph graph, System.Type type, Vector2 position)
        {
            FlowNode node = ScriptableObject.CreateInstance(type) as FlowNode;
            node.hideFlags = HideFlags.HideInHierarchy;
            node.Graph = graph;
            node.Position = new Rect(position, Vector2.zero);
            graph.Nodes.Add(node);

            var nodeTypeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(type);
            if(nodeTypeInfo.OutputType == NodeOutputType.Dynamic)
            {
                var list = System.Activator.CreateInstance(typeof(List<>).MakeGenericType(nodeTypeInfo.DynamicPortType)) as System.Collections.IList;
                var value = nodeTypeInfo.ValueField.GetValue(node);
                nodeTypeInfo.ValueField.SetValue(value, list);
                nodeTypeInfo.DynamicPortField.SetValue(node, list);
            }

            node.OnCreate();
            AssetDatabase.AddObjectToAsset(node, graph);
            EditorUtility.SetDirty(graph);
            return node;
        }
    }
}
