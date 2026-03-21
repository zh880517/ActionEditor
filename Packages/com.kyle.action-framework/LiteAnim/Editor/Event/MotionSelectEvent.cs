using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class MotionSelectEvent : EventBase<MotionSelectEvent>
    {
        public int SelectedIndex { get; private set; }

        public static MotionSelectEvent GetPooled(int selectedIndex)
        {
            var evt = EventBase<MotionSelectEvent>.GetPooled();
            evt.SelectedIndex = selectedIndex;
            return evt;
        }
    }
}
