using UnityEngine.UIElements;

namespace Timeline
{
    public class ClipSelectEvent : EventBase<ClipSelectEvent>
    {
        public string ClipKey { get; private set; }

        protected override void Init()
        {
            base.Init();
            ClipKey = string.Empty;
            bubbles = true;
        }

        public static ClipSelectEvent GetPooled(VisualElement target, string clipKey)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.ClipKey = clipKey;
            evt.bubbles = true;
            return evt;
        }
    }
}
