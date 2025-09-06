using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class FlowDataOutputPort : FlowNodePort
    {
        public string FieldName;
        public FlowDataOutputPort(Type type)
            : base(Orientation.Horizontal, Direction.Output, Capacity.Multi, type)
        {
            var connectorListener = new EdgeConnectorListener();
            m_EdgeConnector = new EdgeConnector<FlowEdgeView>(connectorListener);
            this.AddManipulator(m_EdgeConnector);
            AddToClassList($"Port_{type.Name}");
            AddToClassList($"Port_Out_{type.Name}");
        }
    }
}
