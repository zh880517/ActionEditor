using UnityEditor;

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
    }
}
