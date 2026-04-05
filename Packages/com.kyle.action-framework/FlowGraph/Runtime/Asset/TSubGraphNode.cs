namespace Flow
{
    /// <summary>
    /// 泛型 SubGraph 节点基类。
    /// TData：节点自身配置数据 struct，定义静态数据端口（与 TFlowNode<T> 的 Value 作用相同）。
    /// TGraph：SubGraph 资产类型约束，同时提供动态数据端口。
    /// 节点创建菜单路径由 TData 上的 [FlowNodePath] 控制，Tag 由 TData 实现的接口上的 [FlowTag] 决定。
    /// </summary>
    public class TSubGraphNode<TData, TGraph> : SubGraphNode
        where TData : struct
        where TGraph : FlowSubGraph
    {
        [ExpandedInParent]
        public TData Value;

        /// <summary>
        /// 强类型 SubGraph 访问属性，底层仍序列化为 SubGraphNode.SubGraph 字段
        /// </summary>
        public TGraph TypedSubGraph
        {
            get => base.SubGraph as TGraph;
            set => base.SubGraph = value;
        }

        public override bool IsDefine<T>()
        {
            return base.IsDefine<T>() || Value is T;
        }

        public override FlowNodeRuntimeData Export()
        {
            var baseData = base.Export() as SubGraphNodeRuntimeData;
            var data = new TSubGraphNodeRuntimeData<TData>();
            data.SubGraphName = baseData.SubGraphName;
            data.InputPortEdgeIDs = baseData.InputPortEdgeIDs;
            data.OutputPortEdgeIDs = baseData.OutputPortEdgeIDs;
            data.InputPortSubEdgeIDs = baseData.InputPortSubEdgeIDs;
            data.OutputPortSubEdgeIDs = baseData.OutputPortSubEdgeIDs;
            data.Value = Value;
            return data;
        }
    }
}
