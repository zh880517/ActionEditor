using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class ViewRefeshEvent : EventBase<ViewRefeshEvent>
    {
        public static ViewRefeshEvent GetPooled(VisualElement target)
        {
            var evt = EventBase<ViewRefeshEvent>.GetPooled();
            evt.target = target;
            return evt;
        }

        public static void Dispatch(VisualElement target)
        {
            using var evt = GetPooled(target);
            target.SendEvent(evt);
        }
    }
}
