using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Timeline
{
    public class TrackIndexChangedEvent : EventBase<TrackIndexChangedEvent>
    {
        // 按新顺序排列的 Track Key 列表
        public List<string> OrderedKeys { get; private set; }

        protected override void Init()
        {
            base.Init();
            OrderedKeys = new List<string>();
            bubbles = true;
        }

        public static TrackIndexChangedEvent GetPooled(VisualElement target, List<string> orderedKeys)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.OrderedKeys = orderedKeys;
            evt.bubbles = true;
            return evt;
        }
    }
}
