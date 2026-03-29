using System.Collections.Generic;

namespace Flow
{
    public class FlowMainGraphRuntimeData : FlowGraphRuntimeData
    {
        /// <summary>
        /// 所有被引用的SubGraph名字（包含嵌套SubGraph递归引用的SubGraph）
        /// </summary>
        public List<string> SubGraphNames = new List<string>();
    }
}
