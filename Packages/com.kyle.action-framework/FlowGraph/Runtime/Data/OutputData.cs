namespace Flow
{
   
    public interface IOutputData
    {
        public ulong EdgeID { get; set; }
    }

    //不可变数据，节点执行一次后，数据就固定了
    [System.Serializable]
    public struct OutputData<T> : IOutputData
    {
        public ulong EdgeID 
        { 
            get => Key;
            set => Key = value;
        }
        public ulong Key;
    }

    //可变数据，每次读取都要对应的节点执行一次，重新计算一次数值
    [System.Serializable]
    public struct OutputMutableData<T> : IOutputData
    {
        public ulong EdgeID
        {
            get => Key;
            set => Key = value;
        }
        public ulong Key;
    }
}
