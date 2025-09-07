namespace Flow.EditorView
{
    //[CustomFlowGraphWindow(typeof(CustomFlowType1))]
    //[CustomFlowGraphWindow(typeof(CustomFlowType2))]
    public abstract class TFlowEditorWindow<T> : FlowEditorWindow where T : FlowGraph
    {

    }
}
