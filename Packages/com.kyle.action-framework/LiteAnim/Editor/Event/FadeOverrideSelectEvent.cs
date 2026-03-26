using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class FadeOverrideSelectEvent : EventBase<FadeOverrideSelectEvent>
    {
        public int SelectedIndex { get; private set; }

        public static FadeOverrideSelectEvent GetPooled(int selectedIndex)
        {
            var evt = EventBase<FadeOverrideSelectEvent>.GetPooled();
            evt.SelectedIndex = selectedIndex;
            evt.bubbles = true;
            return evt;
        }
    }
}
