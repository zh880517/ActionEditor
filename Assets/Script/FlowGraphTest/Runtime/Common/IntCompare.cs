using Flow;
using UnityEngine;

public enum CompareType
{
    [InspectorName("=")]
    Equal,
    [InspectorName("!=")]
    NotEqual,
    [InspectorName(">")]
    Greater,
    [InspectorName(">=")]
    GreaterEqual,
    [InspectorName("<")]
    Less,
    [InspectorName("<=")]
    LessEqual
}
[FlowNodePath("比较")]
[Alias("整数比较")]
[System.Serializable]
public struct IntCompare : ICommonConditionable
{
    [Inputable]
    public int A;
    public CompareType Compare;
    [Inputable]
    public int B;
}
