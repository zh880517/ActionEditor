using System.Collections.Generic;
using System.Linq;

namespace Flow.EditorView
{
    public static class FlowPortOperateUtil
    {
        public static void RemoveDynamicOutputPort(FlowNode node, int index)
        {
            if (node == null || !(node is IFlowDynamicOutputable))
                return;
            FlowGraphEditorUtil.RegisterUndo(node, "remove dynamic port", true);
            var graph = node.Graph;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var e = graph.Edges[i];
                if(e.Output == node)
                {
                    if (e.OutputIndex == index)
                    {
                        graph.Edges.RemoveAt(i);
                        i--;
                    }
                    else if (e.OutputIndex > index)
                    {
                        e.OutputIndex--;
                    }
                }
            }
        }

        public static void RemoveDynamicOutputPort(FlowNode node, IEnumerable<int> indexs)
        {
            if (node == null || !(node is IFlowDynamicOutputable))
                return;
            FlowGraphEditorUtil.RegisterUndo(node, "remove dynamic port", true);
            var graph = node.Graph;
            indexs = indexs.OrderByDescending(i => i);
            foreach (var index in indexs)
            {
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    var e = graph.Edges[i];
                    if (e.Output == node)
                    {
                        if (e.OutputIndex == index)
                        {
                            graph.Edges.RemoveAt(i);
                            i--;
                        }
                        else if (e.OutputIndex > index)
                        {
                            e.OutputIndex--;
                        }
                    }
                }
            }
        }

        public static void DynamicOutputPortIndexChanged(FlowNode node, int srcIndex, int dstIndex)
        {
            if (node == null || !(node is IFlowDynamicOutputable))
                return;
            FlowGraphEditorUtil.RegisterUndo(node, "dynamic port index changed", true);
            var graph = node.Graph;
            //先按照移除srcIndex，再插入dstIndex的逻辑处理
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var e = graph.Edges[i];
                if (e.Output == node)
                {
                    if (e.OutputIndex == srcIndex)
                    {
                        e.OutputIndex = dstIndex;
                        continue;
                    }
                    //先处理srcIndex的移除
                    if (e.OutputIndex > srcIndex)
                        e.OutputIndex--;
                    //再处理dstIndex的插入
                    if (e.OutputIndex >= dstIndex)
                        e.OutputIndex++;
                }
            }
        }
    }
}
