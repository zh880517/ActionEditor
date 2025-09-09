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

public struct IntCompare : IFlowInputable, IFlowConditionable
{
    [Inputable]
    public int A;
    public CompareType Compare;
    [Inputable]
    public int B;
}
