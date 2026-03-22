using UnityEngine.UIElements;

namespace Timeline
{
    public enum TrackDragPhase
    {
        Start,
        Drag,
        End
    }

    public class TrackDragEvent : EventBase<TrackDragEvent>
    {
        // 被拖拽的 Track Key
        public string TrackKey { get; private set; }
        // 拖拽阶段
        public TrackDragPhase Phase { get; private set; }
        // 当前鼠标在 trackContainer 坐标系中的 y 值（Start/End 时无意义）
        public float LocalY { get; private set; }

        protected override void Init()
        {
            base.Init();
            TrackKey = string.Empty;
            Phase = TrackDragPhase.Start;
            LocalY = 0f;
            bubbles = true;
        }

        public static TrackDragEvent GetPooled(VisualElement target, string trackKey, TrackDragPhase phase, float localY = 0f)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.TrackKey = trackKey;
            evt.Phase = phase;
            evt.LocalY = localY;
            evt.bubbles = true;
            return evt;
        }
    }
}
