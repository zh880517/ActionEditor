namespace Flow
{
    public class TFlowNode<T> : FlowNode where T : struct
    {
        [ExpandedInParent]
        public T Value;

        public override bool IsDefine<U>()
        {
            return Value is U;
        }

        public override FlowNodeRuntimeData Export()
        {
            var exportData = CreateExport();
            exportData.Value = Value;
            return exportData;
        }

        protected virtual TFlowNodeRuntimeData<T> CreateExport()
        {
            return new TFlowNodeRuntimeData<T>();
        }
    }
}
