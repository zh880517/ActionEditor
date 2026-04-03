using UnityEditor.Experimental.GraphView;

namespace Flow.EditorView
{
    /// <summary>
    /// 统一包装所有节点视图类型，供FlowGraphView管理。
    /// 支持FlowNodeView、SubGraphNodeView、SubGraphInputNodeView、SubGraphOutputNodeView。
    /// </summary>
    public class FlowNodeViewWrapper
    {
        public FlowNode Node { get; private set; }
        public Node View { get; private set; }

        private FlowNodeView nodeView;
        private SubGraphNodeView subGraphView;
        private SubGraphInputNodeView inputNodeView;
        private SubGraphOutputNodeView outputNodeView;

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

        public FlowNodeViewWrapper(SubGraphInputNodeView view)
        {
            inputNodeView = view;
            Node = view.Node;
            View = view;
        }

        public FlowNodeViewWrapper(SubGraphOutputNodeView view)
        {
            outputNodeView = view;
            Node = view.Node;
            View = view;
        }

        public void Refresh()
        {
            nodeView?.Refresh();
            subGraphView?.Refresh();
            inputNodeView?.Refresh();
            outputNodeView?.Refresh();
        }

        public void DisconnectAll()
        {
            nodeView?.DisconnectAll();
            subGraphView?.DisconnectAll();
            inputNodeView?.DisconnectAll();
            outputNodeView?.DisconnectAll();
        }

        public FlowNodePort GetFlowPort(bool isInput, int index)
        {
            return nodeView?.GetFlowPort(isInput, index)
                ?? subGraphView?.GetFlowPort(isInput, index)
                ?? inputNodeView?.GetFlowPort(isInput, index)
                ?? outputNodeView?.GetFlowPort(isInput, index);
        }

        public FlowNodePort GetDataPort(bool isInput, string fieldName)
        {
            return nodeView?.GetDataPort(isInput, fieldName)
                ?? subGraphView?.GetDataPort(isInput, fieldName)
                ?? inputNodeView?.GetDataPort(isInput, fieldName)
                ?? outputNodeView?.GetDataPort(isInput, fieldName);
        }

        public void RefreshDataPorts()
        {
            nodeView?.RefreshDataPorts();
        }
    }
}
