using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleGroupView : VisualElement
    {
        private readonly VisualElement dragLine = new VisualElement();

        public TrackTitleGroupView()
        {
            style.flexDirection = FlexDirection.Column;
            dragLine.style.position = Position.Absolute;
            dragLine.style.width = 2;
            dragLine.style.backgroundColor = Color.white;
            dragLine.style.display = DisplayStyle.None;
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
    }
}
