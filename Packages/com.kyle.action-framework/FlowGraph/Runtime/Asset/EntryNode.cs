using Flow;
[Alias("入口")]
[System.Serializable]
public struct Entry : IFlowEntry
{
}

public class EntryNode : TFlowNode<Entry>
{
}