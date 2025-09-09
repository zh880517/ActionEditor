using Flow;
using System.Collections.Generic;
[FlowNodePath("选项分支")]
[Alias("整数分支")]
[System.Serializable]
public struct IntBranch : ICommonDynamicOutputable
{
    [Inputable]
    public int Value;
    [DynamicOutput]
    public List<int> OutPort;
}