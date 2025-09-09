using Flow;
using System.Collections.Generic;

public struct IntBranch : IFlowDynamicOutputable, IFlowInputable
{
    [Inputable]
    public int Value;
    [DynamicOutput]
    public List<int> OutPort;
}