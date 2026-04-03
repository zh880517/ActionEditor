using Flow.EditorView;
using UnityEditor;

internal class TestFlowSubGraphEditorContext : TFlowGraphEditorContext<TestFlowSubGraph>
{
    public override string AssetRootPath => "Assets";

    protected override void OnExport(TestFlowSubGraph graph)
    {
    }

    [MenuItem("Assets/Create/FlowGraph/TestFlowSubGraph")]
    private static void CreateAction()
    {
        FlowGraphCreateAction.CreateFlowGrah<TestFlowSubGraph>();
    }
}
