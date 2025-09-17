using Flow.EditorView;
using UnityEditor;
using UnityEngine;

internal class TestFlowGraphEditorContext : TFlowGraphEditorContext<TestFlowGraph>
{
    public override string AssetRootPath => "Assets";

    protected override void OnExport(TestFlowGraph graph)
    {
    }
    [MenuItem("Assets/Create/FlowGraph/TestFlowGraph")]
    private static void CreateAction()
    {
        FlowGraphCreateAction.CreateFlowGrah<TestFlowGraph>();
    }
}
