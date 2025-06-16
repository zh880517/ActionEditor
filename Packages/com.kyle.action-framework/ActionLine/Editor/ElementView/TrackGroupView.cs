using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackGroupView : VisualElement
    {
        private readonly List<VisualElement> clipBGs = new List<VisualElement>();

        public TrackGroupView()
        {
            style.flexDirection = FlexDirection.Column;
            style.height = Length.Auto();
            style.flexGrow = 0;
            style.flexShrink = 0;
        }

        public void InsertClip(int index, VisualElement clipView)
        {
            while (clipBGs.Count <= index)
            {
                VisualElement bg = new VisualElement();
                bg.style.marginTop = ActionLineStyles.TrackInterval;
                bg.style.height = ActionLineStyles.ClipHeight;
                bg.style.left = 0;
                bg.style.right = 0;
                bg.style.backgroundColor = ActionLineStyles.GrayBackGroundColor;
                int indeInQueue = clipBGs.Count;
                bg.RegisterCallback<MouseDownEvent>(evt => OnClickBackGround(indeInQueue, evt), TrickleDown.TrickleDown);
                Add(bg);
                clipBGs.Add(bg);
            }
            var clipBG = clipBGs[index];
            if(clipBG.childCount > 0)
            {
               var child = clipBG.ElementAt(0);
                child.RemoveFromHierarchy();
            }
            if (clipView != null)
            {
                clipView.RemoveFromHierarchy();
                clipBG.Add(clipView);
            }
        }

        public void SetClipBGColor(int index, Color color)
        {
            if (index >= 0 && index < clipBGs.Count)
            {
                var clipBG = clipBGs[index];
                clipBG.style.backgroundColor = color;
            }
        }

        private void OnClickBackGround(int index, MouseDownEvent evt)
        {
            SetClipBGColor(index, ActionLineStyles.SelectBackGroundColor);
        }

    }
}
