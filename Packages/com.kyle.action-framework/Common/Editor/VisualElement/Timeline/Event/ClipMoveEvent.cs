using UnityEngine.UIElements;

namespace Timeline
{
    public class ClipMoveEvent : EventBase<ClipMoveEvent>
    {
        public string ClipKey { get; private set; }
        public int OffsetFrame {  get; private set; }

        protected override void Init()
        {
            base.Init();
            OffsetFrame = 0;
            ClipKey = string.Empty;
            bubbles = true;
        }

        public static ClipMoveEvent GetPooled(VisualElement target, string clipKey, int offsetFrame)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.ClipKey = clipKey;
            evt.OffsetFrame = offsetFrame;
            evt.bubbles = true;
            return evt;
        }
    }
}
