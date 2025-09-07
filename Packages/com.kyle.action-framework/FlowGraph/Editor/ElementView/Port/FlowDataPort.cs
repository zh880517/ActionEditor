using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class FlowDataPort : FlowNodePort
    {
        public string FieldName;
        public override bool IsFlowPort => false;
        public FlowDataPort(bool input, Type type)
            : base(Orientation.Horizontal, input ? Direction.Input : Direction.Output, Capacity.Multi, type)
        {
            var connectorListener = new EdgeConnectorListener();
            m_EdgeConnector = new EdgeConnector<FlowEdgeView>(connectorListener);
            this.AddManipulator(m_EdgeConnector);
            AddToClassList($"Port_{type.Name}");
            if(input)
                AddToClassList($"Port_In_{type.Name}");
            else
                AddToClassList($"Port_Out_{type.Name}");
        }
    }
}
