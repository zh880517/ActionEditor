using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackGroupView : VisualElement
    {
        private struct ClipUnit
        {
            public ActionClipView ClipView;
            public VisualElement BG;
        }
        private readonly List<ClipUnit> clips = new List<ClipUnit>();
        private int visableCount = 0;

        public System.Func<int, float> FrameToPosition;
        public TrackGroupView()
        {
            style.flexDirection = FlexDirection.Column;
            style.height = Length.Auto();
        }

        public ActionClipView GetClipView(int index)
        {
            EnsureCapacity(index + 1);
            var unit = clips[index];
            return unit.ClipView;
        }

        public void SetVisableCount(int count)
        {
            if (count == visableCount)
                return;
            visableCount = count;
            EnsureCapacity(count);
            for (int i = 0; i < clips.Count; i++)
            {
                var clipUnit = clips[i];
                if (i < count)
                {
                    clipUnit.BG.style.display = DisplayStyle.Flex;
                }
                else
                {
                    clipUnit.BG.style.display = DisplayStyle.None;
                }
            }
        }

        public void UpdateClipPosition()
        {
            for (int i = 0; i < visableCount; i++)
            {
                var unit = clips[i];
                float start = FrameToPosition(unit.ClipView.StartFrame);
                float end = FrameToPosition(unit.ClipView.EndFrame);
                unit.ClipView.style.left = start;
                unit.ClipView.style.width = start - end;
            }
        }

        public void SetClipBGColor(int index, Color color)
        {
            if (index >= 0 && index < clips.Count)
            {
                var clip = clips[index];
                clip.BG.style.backgroundColor = color;
            }
        }

        private void EnsureCapacity(int count)
        {
            while (clips.Count < count)
            {
                VisualElement bg = new VisualElement();
                bg.style.marginTop = ActionLineStyles.TrackInterval;
                bg.style.height = ActionLineStyles.ClipHeight;
                bg.style.left = 0;
                bg.style.right = 0;
                bg.style.backgroundColor = ActionLineStyles.GrayBackGroundColor;
                bg.style.flexGrow = 0;
                bg.style.flexShrink = 0;
                int indexInQueue = clips.Count;
                bg.RegisterCallback<MouseDownEvent>(evt => OnClickBackGround(indexInQueue, evt), TrickleDown.TrickleDown);
                bg.style.display = DisplayStyle.None;
                Add(bg);
                ActionClipView clip = new ActionClipView { Index = indexInQueue };
                bg.Add(clip);
                clips.Add(new ClipUnit { ClipView = clip, BG = bg });
            }
        }

        private void OnClickBackGround(int index, MouseDownEvent evt)
        {
            SetClipBGColor(index, ActionLineStyles.SelectBackGroundColor);
        }

    }
}
