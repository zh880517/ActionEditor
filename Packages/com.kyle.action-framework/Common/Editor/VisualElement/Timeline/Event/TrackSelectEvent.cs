using UnityEngine.UIElements;

namespace Timeline
{
    /// <summary>
    /// TrackView 被点击选中时向上冒泡，携带 TrackKey
    /// </summary>
    public class TrackSelectEvent : EventBase<TrackSelectEvent>
    {
        public string TrackKey { get; private set; }

        protected override void Init()
        {
            base.Init();
            TrackKey = string.Empty;
            bubbles = true;
        }

        public static TrackSelectEvent GetPooled(VisualElement target, string trackKey)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.TrackKey = trackKey;
            evt.bubbles = true;
            return evt;
        }
    }
}
