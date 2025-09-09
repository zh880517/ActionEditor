using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace Flow.EditorView
{
    public class FlowGraphCreateAction : EndNameEditAction
    {
        public System.Type GraphTyp;
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            string name = Path.GetFileNameWithoutExtension(pathName);
            var graph = CreateInstance(GraphTyp) as FlowGraph;
            graph.name = name;
            var context = FlowGraphEditorContext.GetContext(GraphTyp);
            AssetDatabase.CreateAsset(graph, pathName);
            context?.OnGraphCreate(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
            ProjectWindowUtil.ShowCreatedAsset(graph);
        }
        public static string GetSelectedPathOrFallBack()
        {
            string path = "Assets";
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }
        public static void CreateFlowGrah<T>() where T : FlowGraph
        {
            var action = CreateInstance<FlowGraphCreateAction>();
            action.GraphTyp = typeof(T);
            var icon = MonoScriptUtil.GetTypeIcon(action.GraphTyp);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                action,
                $"{GetSelectedPathOrFallBack()}/New{action.GraphTyp.Name}.asset",
                icon,
                ""
                );
        }
    }
}
