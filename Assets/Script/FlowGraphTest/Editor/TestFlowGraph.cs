using Flow;

[FlowGraphTagsAttrribute("Common")]
public class TestFlowGraph : FlowGraph
{
    public override bool CheckDelete(FlowNode node)
    {
        return !(node is EntryNode);
    }
}
