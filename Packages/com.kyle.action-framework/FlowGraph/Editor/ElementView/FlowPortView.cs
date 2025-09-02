using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class FlowPortView : Port
    {
        public FlowPortView(bool isInput) 
            : base(Orientation.Horizontal, isInput ? Direction.Input : Direction.Output, isInput ? Capacity.Multi : Capacity.Single, typeof(FlowPortView))
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
