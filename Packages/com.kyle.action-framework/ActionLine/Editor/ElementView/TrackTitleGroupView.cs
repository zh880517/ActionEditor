using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleGroupView : VisualElement
    {
        private readonly VisualElement dragLine = new VisualElement();
        private readonly List<TrackTitleView> trackTitles = new List<TrackTitleView>();
        private int visableCount = 0;
        public TrackTitleGroupView()
        {
            style.flexDirection = FlexDirection.Column;
            dragLine.style.position = Position.Absolute;
            dragLine.style.width = 2;
            dragLine.style.backgroundColor = Color.white;
            dragLine.style.display = DisplayStyle.None;
            style.height = Length.Auto();
            Add(dragLine);
        }

        public int GetTrackIndexByMousePosition(Vector2 mousePosition)
        {
            var local = this.WorldToLocal(mousePosition);
            for (int i = 0; i < visableCount; i++)
            {
                var titleView = trackTitles[i];
                var layout = titleView.layout;
                if(layout.yMin > local.y)
                    return i - 1;
                if (layout.yMax > local.y)
                    return i;
            }
            return visableCount;
        }

        public void SetVisableCount(int count)
        {
            if (visableCount == count)
                return;
            EnsureCapacity(count);
            visableCount = count;
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
                    titleView.SetCustomElement(null);
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
                titleView.Index = trackTitles.Count;
                trackTitles.Add(titleView);
                titleView.style.height = ActionLineStyles.ClipHeight;
                titleView.style.marginTop = ActionLineStyles.TrackInterval;
                titleView.style.display = DisplayStyle.None;
                titleView.style.flexGrow = 0;
                titleView.style.flexShrink = 0;
                Add(titleView);
            }
        }

        public void ShowDragLineAfter(int index)
        {
            if(index < 0 || visableCount == 0)
            {
                dragLine.style.top = 0;
                dragLine.style.display = DisplayStyle.Flex;
                return;
            }
            if(index >= visableCount)
            {
                var last = trackTitles[visableCount - 1];
                dragLine.style.top = last.layout.yMax;
                dragLine.style.display = DisplayStyle.Flex;
                return;
            }
            var titleView = trackTitles[index];
            dragLine.style.top = titleView.layout.yMax;
        }
        public void HideDragLine()
        {
            dragLine.style.display = DisplayStyle.None;
        }
    }
}
