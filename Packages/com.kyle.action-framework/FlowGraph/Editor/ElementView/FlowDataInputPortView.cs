using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class FlowDataInputPortView : Port
    {
        public FlowDataInputPortView(Type type)
            : base(Orientation.Horizontal, Direction.Output, Capacity.Single, type)
        {
            var connectorListener = new EdgeConnectorListener();
            m_EdgeConnector = new EdgeConnector<FlowEdgeView>(connectorListener);
            this.AddManipulator(m_EdgeConnector);

            AddToClassList($"Port_{type.Name}");
            AddToClassList($"Port_In_{type.Name}");
        }
    }
}
