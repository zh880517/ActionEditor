using Timeline;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public abstract class MotionEditorView : VisualElement
    {
        public abstract MotionType Type { get; }

        protected readonly ScrollView scrollView;
        protected readonly TimelineView timelineView;

        protected LiteAnimMotion motion;
        protected int selectedIndex = -1;

        protected MotionEditorView(bool trackDragable = false)
        {
            style.flexGrow = 1;
            style.flexShrink = 1;

            scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;
            Add(scrollView);

            timelineView = new TimelineView(trackDragable: trackDragable);
            timelineView.style.flexShrink = 0;
            timelineView.style.marginBottom = 4;
            timelineView.AutoHeight = true;
            scrollView.Add(timelineView);
        }

        public abstract void Refresh(LiteAnimMotion motion);
    }
}
