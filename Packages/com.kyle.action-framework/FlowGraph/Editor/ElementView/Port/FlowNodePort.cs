using System;
using UnityEditor.Experimental.GraphView;

namespace Flow.EditorView
{
    public abstract class FlowNodePort : Port
    {
        public FlowNode Owner;
        public abstract bool IsFlowPort { get; }
        protected FlowNodePort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
        }
    }
}
