using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackView : VisualElement
    {
        public ActionClipView ClipView { get; private set; }
        private readonly VisualElement disbaleElement = new VisualElement();
        public TrackView()
        {
            style.backgroundColor = ActionLineStyles.TrackGray;
            Add(disbaleElement);
            disbaleElement.StretchToParentSize();
            disbaleElement.style.backgroundColor = new Color(0, 0, 0, 0.9f);
            disbaleElement.style.display = DisplayStyle.None;

            disbaleElement.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                style.backgroundColor = ActionLineStyles.TrackBlue;
            }
            else
            {
                style.backgroundColor = ActionLineStyles.TrackGray;
            }
        }

        public void SetDisabled(bool disabled)
        {
            if (disabled)
            {
                disbaleElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                disbaleElement.style.display = DisplayStyle.None;
            }
        }

        public void SetClipView(ActionClipView clipView)
        {
            if(clipView != ClipView)
            {
                ClipView?.RemoveFromHierarchy();
            }
            if (clipView != null)
            {
                clipView.style.position = Position.Absolute;
                clipView.style.top = 0;
                clipView.style.bottom = 0;
                Insert(0, clipView);//确保ClipView能被disbaleElement遮住
            }
            ClipView = clipView;
        }
    }
}
