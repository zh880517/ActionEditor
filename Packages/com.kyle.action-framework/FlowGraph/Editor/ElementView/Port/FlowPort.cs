using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class FlowPort : FlowNodePort
    {
        public int Index;
        public override bool IsFlowPort => true;
        public bool IsInput => direction == Direction.Input;

        public FlowPort(bool isInput) 
            : base(Orientation.Horizontal, isInput ? Direction.Input : Direction.Output, isInput ? Capacity.Multi : Capacity.Single, typeof(FlowPort))
        {
            var connectorListener = new EdgeConnectorListener();
            m_EdgeConnector = new EdgeConnector<FlowEdgeView>(connectorListener);
            this.AddManipulator(m_EdgeConnector);
            if (isInput)
            {
                AddToClassList("Port_In");
            }
            else
            {
                AddToClassList("Port_Out");
            }
        }
    }
}
