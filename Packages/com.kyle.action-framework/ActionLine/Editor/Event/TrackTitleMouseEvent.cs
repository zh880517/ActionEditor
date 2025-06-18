using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleMouseEvent<T> : EventBase<T> where T : TrackTitleMouseEvent<T>, new()
    {
        public EventModifiers Modifiers { get; private set; }
        public int Button { get; private set; }
        public int Index { get; private set; }
        public Vector2 MousePositon { get; private set; }
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

        public static T GetPooled(int button, int index, Vector2 mousePosition, EventModifiers modifiers)
        {
            var evt = GetPooled();
            evt.Button = button;
            evt.Index = index;
            evt.MousePositon = mousePosition;
            evt.Modifiers = modifiers;
            return evt;
        }
    }

    public class TrackTitleMouseDownEvent : TrackTitleMouseEvent<TrackTitleMouseDownEvent>
    {
    }

    public class TrackTitleMouseMoveEvent : TrackTitleMouseEvent<TrackTitleMouseMoveEvent>
    {
    }

    public class TrackTitleMouseUpEvent : TrackTitleMouseEvent<TrackTitleMouseUpEvent>
    {
    }
}
