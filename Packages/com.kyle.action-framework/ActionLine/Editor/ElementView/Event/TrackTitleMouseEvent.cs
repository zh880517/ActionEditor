using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleMouseEvent<T> : EventBase<T> where T : TrackTitleMouseEvent<T>, new()
    {
        public int Button { get; private set; }
        public int Index { get; private set; }
        public Vector2 MousePositon { get; private set; }
        protected override void Init()
        {
            base.Init();
            bubbles = true;
        }

        public static T GetPooled(int button, int index, Vector2 mousePosition)
        {
            var evt = GetPooled();
            evt.Button = button;
            evt.Index = index;
            evt.MousePositon = mousePosition;
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
