using System;
using UnityEditor.Experimental.GraphView;

namespace Flow.EditorView
{
    public abstract class FlowNodePort : Port
    {
        public FlowNode Owner;
        protected FlowNodePort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
        }
    }
}
