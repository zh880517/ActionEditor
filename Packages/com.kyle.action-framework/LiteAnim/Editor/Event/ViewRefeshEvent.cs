using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class ViewRefeshEvent : EventBase<ViewRefeshEvent>
    {
        public static ViewRefeshEvent GetPooled(VisualElement target)
        {
            var evt = EventBase<ViewRefeshEvent>.GetPooled();
            evt.target = target;
            evt.bubbles = true;
            return evt;
        }

        public static void Dispatch(VisualElement target)
        {
            using var evt = GetPooled(target);
            target.SendEvent(evt);
        }
    }
}
