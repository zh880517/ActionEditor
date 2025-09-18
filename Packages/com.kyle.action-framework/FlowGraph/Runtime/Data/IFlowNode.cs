namespace Flow
{
    public interface IFlowNode
    {
    }
    public interface IFlowInputable : IFlowNode
    {
    }

    //数据提供节点，无Flow端口，只有数据端口
    public interface IFlowDataProvider : IFlowNode
    {
        bool IsRealTimeData { get; }//是否为实时数据提供者,如果是则每次请求数据都调用，否则只调用一次缓存结果
    }


    public interface IFlowOutputable : IFlowNode
    {
    }

    public interface IFlowEntry : IFlowOutputable
    {
    }

    //Update节点
    public interface IFlowUpdateable : IFlowInputable, IFlowOutputable
    {
    }

    //条件节点，有两个输出，0是true，1是false
    public interface IFlowConditionable : IFlowOutputable
    {
    }

    //动态输出节点，可以动态添加输出端口
    //输出为索引，不符合的默认输出为-1；
    public interface IFlowDynamicOutputable : IFlowOutputable
    {
    }
}
