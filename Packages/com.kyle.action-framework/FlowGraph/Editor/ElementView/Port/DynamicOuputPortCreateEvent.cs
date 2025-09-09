using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class DynamicOuputPortCreateEvent : EventBase<DynamicOuputPortCreateEvent>
    {
        public FlowPort Port { get; private set; }
        public FlowNode Node { get; private set; }
        public int Index { get; private set; }

        protected override void Init()
        {
            base.Init();
            bubbles = true;
        }

        public static DynamicOuputPortCreateEvent GetPooled(FlowPort port, FlowNode node, int index)
        {
            var e = GetPooled();
            e.target = port;
            e.Port = port;
            e.Node = node;
            e.Index = index;
            return e;
        }
    }
}
