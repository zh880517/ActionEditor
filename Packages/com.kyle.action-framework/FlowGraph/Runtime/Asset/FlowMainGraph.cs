using System.Collections.Generic;

namespace Flow
{
    public class FlowMainGraph : FlowGraph
    {
        public override void ExportToRuntime(FlowGraphRuntimeData data)
        {
            base.ExportToRuntime(data);
            if (data is FlowMainGraphRuntimeData mainData)
            {
                mainData.SubGraphNames.Clear();
                CollectSubGraphNames(this, mainData.SubGraphNames, new HashSet<string>());
            }
        }

        /// <summary>
        /// 递归收集所有SubGraph名字，包含嵌套SubGraph引用的SubGraph。
        /// 通过遍历FlowGraph资产中的SubGraphNode递归，visited防止循环引用。
        /// </summary>
        private static void CollectSubGraphNames(FlowGraph graph, List<string> names, HashSet<string> visited)
        {
            foreach (var node in graph.Nodes)
            {
                if (node is SubGraphNode subNode && subNode.SubGraph != null)
                {
                    string subName = subNode.SubGraph.name;
                    if (!string.IsNullOrEmpty(subName) && visited.Add(subName))
                    {
                        names.Add(subName);
                        // 递归收集嵌套SubGraph
                        CollectSubGraphNames(subNode.SubGraph, names, visited);
                    }
                }
            }
        }
    }
}
