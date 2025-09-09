using Flow;

public struct IntAdd : IFlowOutputable
{
    [Inputable]
    public int A;
    [Inputable]
    public int B;
    public OutputData<int> Result;
}
