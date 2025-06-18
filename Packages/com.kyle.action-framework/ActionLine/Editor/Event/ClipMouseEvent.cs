using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ClipMouseEvent<T> : EventBase<T> where T : ClipMouseEvent<T>, new()
    {
        public EventModifiers Modifiers { get; private set; }
        public int Button { get; private set; }//-1 左，0 中，1 右
        public int Type { get; private set; }
        public int ClipIndex { get; private set; }
        public Vector2 MousePosition { get; private set; }

        public bool ShiftKey => (Modifiers & EventModifiers.Shift) != 0;

        public bool CtrlKey => (Modifiers & EventModifiers.Control) != 0;

        public bool CommandKey => (Modifiers & EventModifiers.Command) != 0;

        public bool AltKey => (Modifiers & EventModifiers.Alt) != 0;

        public bool ActionKey
        {
            get
            {
                if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                {
                    return CommandKey;
                }

                return CtrlKey;
            }
        }

        protected override void Init()
        {
            base.Init();
            bubbles = true;
        }
        public static T GetPooled(int button, int type, int index, Vector2 mousePosition, EventModifiers modifiers)
        {
            var evt = GetPooled();
            evt.Button = button;
            evt.Type = type;
            evt.ClipIndex = index;
            evt.MousePosition = mousePosition;
            evt.Modifiers = modifiers;
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
