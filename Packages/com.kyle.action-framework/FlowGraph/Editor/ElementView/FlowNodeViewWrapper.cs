using UnityEditor.Experimental.GraphView;

namespace Flow.EditorView
{
    /// <summary>
    /// 统一包装所有节点视图类型，供FlowGraphView管理。
    /// 支持FlowNodeView和SubGraphNodeView。
    /// </summary>
    public class FlowNodeViewWrapper
    {
        public FlowNode Node { get; private set; }
        public Node View { get; private set; }

        private FlowNodeView nodeView;
        private SubGraphNodeView subGraphView;

        public FlowNodeViewWrapper(FlowNodeView view)
        {
            nodeView = view;
            Node = view.Node;
            View = view;
        }

        public FlowNodeViewWrapper(SubGraphNodeView view)
        {
            subGraphView = view;
            Node = view.Node;
            View = view;
        }

        public void Refresh()
        {
            nodeView?.Refresh();
            subGraphView?.Refresh();
        }

        public void DisconnectAll()
        {
            nodeView?.DisconnectAll();
            subGraphView?.DisconnectAll();
        }

        public FlowNodePort GetFlowPort(bool isInput, int index)
        {
            return nodeView?.GetFlowPort(isInput, index)
                ?? subGraphView?.GetFlowPort(isInput, index);
        }

        public FlowNodePort GetDataPort(bool isInput, string fieldName)
        {
            return nodeView?.GetDataPort(isInput, fieldName)
                ?? subGraphView?.GetDataPort(isInput, fieldName);
        }

        public void RefreshDataPorts()
        {
            nodeView?.RefreshDataPorts();
        }
    }
}
