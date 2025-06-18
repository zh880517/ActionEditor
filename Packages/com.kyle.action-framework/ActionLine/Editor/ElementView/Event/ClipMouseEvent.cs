using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ClipMouseEvent<T> : EventBase<T> where T : ClipMouseEvent<T>, new()
    {
        public int Button { get; private set; }
        public int Type { get; private set; }
        public int ClipIndex { get; private set; }
        public Vector2 MousePosition { get; private set; }
        protected override void Init()
        {
            base.Init();
            bubbles = true;
        }
        public static T GetPooled(int button, int type, int index, Vector2 mousePosition)
        {
            var evt = GetPooled();
            evt.Button = button;
            evt.Type = type;
            evt.ClipIndex = index;
            evt.MousePosition = mousePosition;
            return evt;
        }
    }

    public class ClipMouseDownEvent : ClipMouseEvent<ClipMouseDownEvent>
    {
    }

    public class ClipMouseMoveEvent : ClipMouseEvent<ClipMouseMoveEvent>
    {
    }

    public class ClipMouseUpEvent : ClipMouseEvent<ClipMouseUpEvent>
    {
    }
}
