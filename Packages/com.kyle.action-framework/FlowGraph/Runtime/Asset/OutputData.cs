namespace Flow
{
   
    public interface IOutputData
    {
        System.Type DataType { get; }
    }

    //不可变数据，节点执行一次后，数据就固定了
    [System.Serializable]
    public struct OutputData<T> : IOutputData
    {
        public readonly System.Type DataType => typeof(T);
        public int Key;//编辑器不使用
    }

    //可变数据，每次读取都要对应的节点执行一次，重新计算一次数值
    [System.Serializable]
    public struct OutputMutableData<T> : IOutputData
    {
        public readonly System.Type DataType => typeof(T);
        public int Key;//编辑器不使用
        public int NodeID;//编辑器不使用
    }
}
