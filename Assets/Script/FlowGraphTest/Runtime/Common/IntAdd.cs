using Flow;

[FlowNodePath("数值操作")]
[Alias("整数加法")]
[System.Serializable]
public struct IntAdd : ICommonNormalNode
{
    [Inputable]
    public int A;
    [Inputable]
    public int B;
    public OutputData<int> Result;
}
