using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackScrollView : VisualElement
    {
        private readonly TimelineTickMarkView timelineTickMarkView = new TimelineTickMarkView();
        private readonly TimelineCursorView cursorView = new TimelineCursorView();
        private readonly MinMaxSlider minMaxSlider = new MinMaxSlider();
        private readonly Scroller scroller = new Scroller();
        private readonly VisualElement trackListView = new VisualElement();

        public TrackScrollView()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;
            var center = new VisualElement();
            Add(center);
            center.style.position = Position.Absolute;
            center.style.left = 0;
            center.style.right = 20;
            center.style.top = 0;
            center.style.bottom = 20;
            center.style.overflow = Overflow.Hidden;
            center.RegisterCallback<WheelEvent>(OnWheelEvent);

            scroller.AlignParentRight(20);
            scroller.lowValue = 0;
            scroller.highValue = 100;
            scroller.value = 0;
            scroller.direction = SliderDirection.Vertical;
            scroller.style.bottom = 20;
            Add(scroller);

            minMaxSlider.AlignParentBottom(20);
            minMaxSlider.style.right = 20;
            minMaxSlider.value = new Vector2(0, 100);
            minMaxSlider.lowLimit = 0;
            minMaxSlider.highLimit = 100;
            Add(minMaxSlider);

            //时间轴
            center.Add(timelineTickMarkView);
            timelineTickMarkView.StretchToParentSize();
            timelineTickMarkView.SetCursorView(cursorView);
            //轨道区域
            center.Add(trackListView);
            trackListView.StretchToParentSize();
            trackListView.style.top = timelineTickMarkView.TitleHeight;
            trackListView.style.overflow = Overflow.Hidden;
            //时间轴游标
            center.Add(cursorView);
            cursorView.StretchToParentSize();
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            //滚轮事件
        }
    }
}
