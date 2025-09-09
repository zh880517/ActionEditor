using Flow;

[FlowNodePath("��ֵ����")]
[Alias("�����ӷ�")]
[System.Serializable]
public struct IntAdd : ICommonNormalNode
{
    [Inputable]
    public int A;
    [Inputable]
    public int B;
    public OutputData<int> Result;
}
