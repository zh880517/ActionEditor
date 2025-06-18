using System.Collections.Generic;
using UnityEngine;
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
            int index = Mathf.FloorToInt(local.y / (ActionLineStyles.ClipHeight + ActionLineStyles.TrackInterval));
            return Mathf.Clamp(index, 0, visableCount - 1);
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
            float top = (ActionLineStyles.ClipHeight + ActionLineStyles.TrackInterval) * index;
            dragLine.style.top = top;
            dragLine.style.display = DisplayStyle.Flex;
        }
        public void HideDragLine()
        {
            dragLine.style.display = DisplayStyle.None;
        }
    }
}
