using Flow.EditorView;
using UnityEditor;
using UnityEngine;

internal class TestFlowGraphEditorContext : TFlowGraphEditorContext<TestFlowGraph>
{
    public override string AssetRootPath => "Assets";

    protected override void OnCreate(TestFlowGraph graph)
    {
        FlowGraphEditorUtil.CreateNode<EntryNode>(graph, Vector2.zero);
    }

    protected override void OnExport(TestFlowGraph graph)
    {
    }
    [MenuItem("Assets/Create/FlowGraph/TestFlowGraph")]
    private static void CreateAction()
    {
        FlowGraphCreateAction.CreateFlowGrah<TestFlowGraph>();
    }
}
