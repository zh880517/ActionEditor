using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleGroupView : VisualElement
    {
        private readonly VisualElement dragLine = new VisualElement();
        private readonly List<TrackTitleView> trackTitles = new List<TrackTitleView>();
        public TrackTitleGroupView()
        {
            style.flexDirection = FlexDirection.Column;
            dragLine.style.position = Position.Absolute;
            dragLine.style.width = 2;
            dragLine.style.backgroundColor = Color.white;
            dragLine.style.display = DisplayStyle.None;
            style.flexGrow = 1;
            style.flexShrink = 1;
            Add(dragLine);
        }

        public void OnClipMouseDown(TrackTitleView clip, MouseDownEvent evt)
        {

        }
        public void OnClipMouseUp(TrackTitleView clip, MouseUpEvent evt)
        {
            dragLine.style.display = DisplayStyle.None;
        }

        public void OnClipMouseEnter(TrackTitleView clip, MouseEnterEvent evt)
        {
            dragLine.style.top = clip.layout.max.y;
            dragLine.style.display = DisplayStyle.Flex;
        }

        public void SetVisableCount(int count)
        {
            EnsureCapacity(count);
            for (int i = 0; i < trackTitles.Count; i++)
            {
                TrackTitleView titleView = trackTitles[i];
                if (i < count)
                {
                    titleView.style.display = DisplayStyle.Flex;
                }
                else
                {
                    titleView.style.display = DisplayStyle.None;
                }
            }
        }

        public TrackTitleView GetTitleView(int index)
        {
            EnsureCapacity(index + 1);
            return trackTitles[index];
        }

        private void EnsureCapacity(int count)
        {
            while (trackTitles.Count < count)
            {
                TrackTitleView titleView = new TrackTitleView();
                titleView.Root = this;
                trackTitles.Add(titleView);
                titleView.style.height = ActionLineStyles.ClipHeight;
                titleView.style.marginTop = ActionLineStyles.TrackInterval;
                titleView.style.display = DisplayStyle.None;
                Add(titleView);
            }
        }

        public void ShowDragLineAfter(TrackTitleView clip)
        {
            if (clip != null)
            {
                dragLine.style.top = clip.layout.max.y;
            }
            else
            {
                dragLine.style.top = 0;
            }
            dragLine.style.display = DisplayStyle.Flex;
        }
        public void HideDragLine()
        {
            dragLine.style.display = DisplayStyle.None;
        }
    }
}
