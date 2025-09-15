
[TypeCatalog("Common")]
[Flow.FlowTag("Common")]
public interface ICommonNode : Flow.IFlowNode
{
}

public interface ICommonDataProvider : ICommonNode, Flow.IFlowDataProvider
{
}

public interface ICommonOutputable : ICommonNode, Flow.IFlowOutputable
{
}

public interface ICommonInputable : ICommonNode, Flow.IFlowInputable
{
}

public interface ICommonNormalNode : ICommonInputable, ICommonOutputable
{
}

public interface ICommonConditionable : ICommonNode, Flow.IFlowConditionable, Flow.IFlowInputable
{
}

public interface ICommonDynamicOutputable : ICommonNode, Flow.IFlowDynamicOutputable, Flow.IFlowInputable
{
}

public interface ICommonUpdateable : ICommonNode, Flow.IFlowUpdateable
{
}