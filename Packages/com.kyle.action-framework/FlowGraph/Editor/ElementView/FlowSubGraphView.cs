using UnityEditor;
using UnityEngine;

namespace Flow.EditorView
{
    /// <summary>
    /// 子图编辑视图。包含SubGraphInputNode（输入节点）和SubGraphOutputNode（输出节点），
    /// 动态端口添加到对应节点上，通过标准GraphView连线系统与子图内部节点连接。
    /// </summary>
    public class FlowSubGraphView : FlowGraphView
    {
        public FlowSubGraph SubGraph => Graph as FlowSubGraph;

        public FlowSubGraphView(FlowSubGraph graph) : base(graph)
        {
            EnsureIONodes(graph);
        }

        /// <summary>
        /// 确保子图拥有InputNode和OutputNode，若不存在则自动创建
        /// </summary>
        private void EnsureIONodes(FlowSubGraph graph)
        {
            if (graph.InputNode == null)
            {
                var node = FlowGraphEditorUtil.CreateNode<SubGraphInputNode>(graph, new Vector2(-300, 0));
                graph.InputNode = node as SubGraphInputNode;
                EditorUtility.SetDirty(graph);
            }
            if (graph.OutputNode == null)
            {
                var node = FlowGraphEditorUtil.CreateNode<SubGraphOutputNode>(graph, new Vector2(500, 0));
                graph.OutputNode = node as SubGraphOutputNode;
                EditorUtility.SetDirty(graph);
            }
        }

        protected override FlowNodeViewWrapper CreateNodeViewWrapper(FlowNode node)
        {
            if (node is SubGraphInputNode inputNode)
            {
                var view = new SubGraphInputNodeView(inputNode, SubGraph);
                view.OnPortListChanged += () => Refresh();
                return new FlowNodeViewWrapper(view);
            }
            if (node is SubGraphOutputNode outputNode)
            {
                var view = new SubGraphOutputNodeView(outputNode, SubGraph);
                view.OnPortListChanged += () => Refresh();
                return new FlowNodeViewWrapper(view);
            }
            return base.CreateNodeViewWrapper(node);
        }
    }
}
