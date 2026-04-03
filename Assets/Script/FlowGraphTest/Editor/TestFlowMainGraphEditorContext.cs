using Flow.EditorView;
using UnityEditor;

internal class TestFlowMainGraphEditorContext : TFlowGraphEditorContext<TestFlowMainGraph>
{
    public override string AssetRootPath => "Assets";

    protected override void OnExport(TestFlowMainGraph graph)
    {
    }

    [MenuItem("Assets/Create/FlowGraph/TestFlowMainGraph")]
    private static void CreateAction()
    {
        FlowGraphCreateAction.CreateFlowGrah<TestFlowMainGraph>();
    }
}
