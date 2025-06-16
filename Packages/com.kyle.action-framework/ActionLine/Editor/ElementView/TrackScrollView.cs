using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackScrollView : VisualElement
    {
        private readonly TimelineTickMarkView timelineTickMarkView = new TimelineTickMarkView();
        private readonly TimelineCursorView cursorView = new TimelineCursorView();
        private readonly MinMaxSlider minMaxSlider = new MinMaxSlider();
        private readonly Scroller scroller = new Scroller(0, 100, null, SliderDirection.Vertical);
        private readonly VisualElement trackClipArea = new VisualElement();
        private readonly TrackGroupView trackGroup = new TrackGroupView();

        public TimelineTickMarkView TickMarkView => timelineTickMarkView;
        public TrackGroupView TrackGroup => trackGroup;
        private float scale = 1.0f;

        public TrackScrollView()
        {
            style.overflow = Overflow.Hidden;
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;

            // 垂直滚动条样式
            scroller.AlignParentRight(20);
            scroller.style.bottom = 20;
            scroller.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);
            Add(scroller);

            // 水平滚动条
            minMaxSlider.AlignParentBottom(20);
            minMaxSlider.style.right = 20;
            minMaxSlider.value = new Vector2(0, 100);
            minMaxSlider.lowLimit = 0;
            minMaxSlider.highLimit = 100;
            minMaxSlider.RegisterValueChangedCallback(OnHorizontalScroll);
            Add(minMaxSlider);

            // 中心显示区域
            var center = new VisualElement();
            Add(center);
            center.style.position = Position.Absolute;
            center.style.left = 0;
            center.style.right = 20;
            center.style.top = 0;
            center.style.bottom = 20;
            center.RegisterCallback<WheelEvent>(OnWheelEvent);
            center.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);


            //时间轴
            center.Add(timelineTickMarkView);
            timelineTickMarkView.StretchToParentSize();
            timelineTickMarkView.SetCursorView(cursorView);
            timelineTickMarkView.HeaderInterval = ActionLineStyles.TrackHeaderInterval;
            //轨道裁剪区域
            center.Add(trackClipArea);
            trackClipArea.StretchToParentSize();
            trackClipArea.style.top = timelineTickMarkView.TitleHeight;
            trackClipArea.style.overflow = Overflow.Hidden;
            // 轨道组
            trackClipArea.Add(trackGroup);
            //时间轴游标
            center.Add(cursorView);
            cursorView.StretchToParentSize();

            for (int i = 0; i < 20; i++)
            {
                trackGroup.InsertClip(i, null);
            }
        }

        public void SetFrameCount(int frameCount)
        {
            timelineTickMarkView.FrameCount = frameCount;
            if (frameCount > 0)
            {
                trackGroup.style.width = frameCount * ActionLineStyles.FrameWidth * scale + ActionLineStyles.TrackTailInterval;
            }
            else
            {
                trackGroup.style.right = 0;
            }
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            float v = Mathf.Sign(evt.delta.y) * 0.1f;
            scale -= v;
            scale = Mathf.Clamp(scale, 0.1f, 10f);
            UpdateScale();
        }

        private float GetUnScaleTrackWidth()
        {
            if (timelineTickMarkView.FrameCount > 0)
            {
                return timelineTickMarkView.FrameCount * ActionLineStyles.FrameWidth + ActionLineStyles.TrackTailInterval + ActionLineStyles.TrackHeaderInterval;
            }
            return trackClipArea.localBound.size.x;
        }

        private void OnHorizontalScroll(ChangeEvent<Vector2> evt)
        {
            Vector2 viewSize = trackClipArea.localBound.size;
            Vector2 trackGroupSize = trackGroup.localBound.size;
            float newScale = (evt.newValue.y - evt.newValue.x);

            float startX = (evt.newValue.x * 0.01f) * trackGroupSize.x;
            timelineTickMarkView.HorizontalOffset = startX;
            trackGroup.style.left = -startX;
        }

        private void OnVerticalScroll(ChangeEvent<float> evt)
        {
            Vector2 trackGroupSize = trackGroup.localBound.size;
            Vector2 viewSize = trackClipArea.localBound.size;
            float range = trackGroupSize.y - viewSize.y;
            if(range < 0)
            {
                scroller.slider.SetValueWithoutNotify(0);
                trackGroup.style.top = 0;
                return;
            }
            float y = range * evt.newValue * 0.01f;
            trackGroup.style.top = -y;
        }

        private void UpdateScale()
        {
            timelineTickMarkView.Scale = scale;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
        }
    }
}
