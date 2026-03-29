using System.Collections.Generic;

namespace Flow
{
    public class SubGraphNodeRuntimeData : FlowNodeRuntimeData
    {
        /// <summary>
        /// 引用的SubGraph名字，运行时通过FlowSubGraphProvider获取FlowGraphRuntimeData
        /// </summary>
        public string SubGraphName;
        // 父图端口GUID -> 父图中的EdgeID（用于从父图变量池读取Input数据）
        public Dictionary<string, ulong> InputPortEdgeIDs = new Dictionary<string, ulong>();
        // 父图端口GUID -> 父图中的EdgeID（用于向父图变量池写回Output数据）
        public Dictionary<string, ulong> OutputPortEdgeIDs = new Dictionary<string, ulong>();
        // 子图Input端口GUID -> 子图内部DataEdge的EdgeID（端口贴边连线到子图内节点）
        public Dictionary<string, ulong> InputPortSubEdgeIDs = new Dictionary<string, ulong>();
        // 子图Output端口GUID -> 子图内部DataEdge的EdgeID（子图内节点连线到端口）
        public Dictionary<string, ulong> OutputPortSubEdgeIDs = new Dictionary<string, ulong>();

        public override IFlowNodeExecutor Executor => SubGraphNodeExecutor.Instance;
    }
}
