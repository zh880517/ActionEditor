﻿using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackScrollView : VisualElement
    {
        private readonly TimelineTickMarkView timelineTickMarkView = new TimelineTickMarkView();
        private readonly TimelineCursorView cursorView = new TimelineCursorView();
        private readonly MinMaxSlider horizontalSlider = new MinMaxSlider();
        private readonly Scroller verticalSlider = new Scroller(0, 100, null, SliderDirection.Vertical);
        private readonly VisualElement trackClipArea = new VisualElement();
        private readonly TrackGroupView trackGroup = new TrackGroupView();
        private bool hasGeometryChange;

        public TimelineTickMarkView TickMarkView => timelineTickMarkView;
        public TrackGroupView Group => trackGroup;
        private float viewScale = 1.0f;
        private float horizontalOffset = 0;
        private float verticalOffset = 0;
        public float Scale => viewScale;
        public float HorizontalOffset => horizontalOffset;
        public float VerticalOffset => verticalOffset;

        public int CurrentFrame => cursorView.CurrentFrame;


        public System.Action<float> OnScaleChanged;
        public System.Action<float> OnVerticalScrollChanged;

        public TrackScrollView()
        {
            pickingMode = PickingMode.Ignore;
            style.overflow = Overflow.Hidden;
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;

            // 垂直滚动条样式
            verticalSlider.AlignParentRight(20);
            verticalSlider.style.bottom = 20;
            verticalSlider.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);
            Add(verticalSlider);

            // 水平滚动条
            horizontalSlider.AlignParentBottom(20);
            horizontalSlider.style.right = 20;
            horizontalSlider.style.left = 20;
            horizontalSlider.value = new Vector2(0, 100);
            horizontalSlider.lowLimit = 0;
            horizontalSlider.highLimit = 100;
            horizontalSlider.RegisterValueChangedCallback(OnHorizontalScroll);
            Add(horizontalSlider);

            // 中心显示区域
            var center = new VisualElement();
            center.pickingMode = PickingMode.Ignore;
            Add(center);
            center.style.overflow = Overflow.Hidden;
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
            timelineTickMarkView.TitleHeight = ActionLineStyles.TitleBarHeight;
            timelineTickMarkView.SetCursorView(cursorView);
            timelineTickMarkView.HeaderInterval = ActionLineStyles.TrackHeaderInterval;
            //轨道裁剪区域
            trackClipArea.pickingMode = PickingMode.Ignore;
            center.Add(trackClipArea);
            trackClipArea.StretchToParentSize();
            trackClipArea.style.top = ActionLineStyles.TitleBarHeight;
            trackClipArea.style.overflow = Overflow.Hidden;
            // 轨道组
            trackGroup.FrameToPosition = FrameToPosition;
            trackClipArea.Add(trackGroup);
            //时间轴游标
            center.Add(cursorView);
            cursorView.StretchToParentSize();
        }

        public void SetFrameCount(int frameCount)
        {
            if (frameCount <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(frameCount), "Frame count must be greater than zero.");
            }
            timelineTickMarkView.FrameCount = frameCount;
            trackGroup.style.width = frameCount * ActionLineStyles.FrameWidth * viewScale + ActionLineStyles.TrackTailInterval;
            if (hasGeometryChange)
            {
                UpdateHorizontalSliderRange();
            }
        }

        public void SetFrameIndex(int frame)
        {
            cursorView.CurrentFrame = frame;
        }

        public void FitFrameInView(int frame)
        {
            if (!hasGeometryChange)
                return;
            float framePosition = ActionLineStyles.FrameInTrackPosition(frame, viewScale);
            float viewVisualWidth = trackClipArea.localBound.size.x;
            float viewWidth = viewVisualWidth / viewScale;
            float offset = Mathf.Min(40, viewVisualWidth * 0.1f);
            if (framePosition > horizontalOffset && framePosition < viewWidth + horizontalOffset)
            {
                // 如果帧位置在当前视图范围内，则不需要滚动
                return;
            }
            if (framePosition < horizontalOffset)
            {
                float x = (framePosition - offset) / GetUnScaleTrackWidth();
                var previousValue = horizontalSlider.value;
                x *= 100;
                x = Mathf.Max(0, x);
                horizontalSlider.value = new Vector2(x, x + (previousValue.y - previousValue.x));
            }
            else
            {
                float x = (framePosition - viewWidth + offset) / GetUnScaleTrackWidth();
                var previousValue = horizontalSlider.value;
                x *= 100;
                x = Mathf.Max(0, x);
                horizontalSlider.value = new Vector2(x, x + (previousValue.y - previousValue.x));
            }
        }

        public void SetViewPort(float scale, float hOffset, float vOffset)
        {
            viewScale = Mathf.Clamp(scale, 0.1f, 10f);
            horizontalOffset = Mathf.Max(0, hOffset);
            timelineTickMarkView.HorizontalOffset = horizontalOffset;

            verticalOffset = Mathf.Max(0, vOffset);
            Vector2 trackGroupSize = trackGroup.localBound.size;
            Vector2 viewSize = trackClipArea.localBound.size;
            float range = trackGroupSize.y - viewSize.y;
            verticalSlider.slider.SetValueWithoutNotify((verticalOffset * 100) / range);
            trackGroup.style.top = -verticalOffset;

            OnScaleChange();
        }

        public int GetFrameInTrackByMousePosition(Vector2 position)
        {
            var local = trackGroup.WorldToLocal(position);
            local.x /= viewScale;
            local.x += horizontalOffset;
            local.x -= ActionLineStyles.TrackHeaderInterval;

            return Mathf.FloorToInt(local.x / ActionLineStyles.FrameWidth);
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            float v = Mathf.Sign(evt.delta.y) * 0.1f;
            viewScale -= v;
            viewScale = Mathf.Clamp(viewScale, 0.1f, 10f);
            OnScaleChange();
        }

        private float GetUnScaleTrackWidth()
        {
            return timelineTickMarkView.FrameCount * ActionLineStyles.FrameWidth + ActionLineStyles.TrackTailInterval + ActionLineStyles.TrackHeaderInterval;
        }

        private void OnHorizontalScroll(ChangeEvent<Vector2> evt)
        {
            float trackWidth = GetUnScaleTrackWidth();
            Vector2 viewSize = trackClipArea.localBound.size;
            float viewShowRate = viewSize.x / trackWidth;
            float newViewShowRate = (evt.newValue.y - evt.newValue.x) * 0.01f;
            float newScale = viewShowRate / newViewShowRate;
            if(newScale < 0.1f || newScale > 10)
            {
                horizontalSlider.SetValueWithoutNotify(evt.previousValue);
                return;
            }
            viewScale = newScale;
            horizontalOffset = (evt.newValue.x * 0.01f) * trackWidth; 
            timelineTickMarkView.HorizontalOffset = horizontalOffset;
            OnScaleChange(false);
        }

        private void OnVerticalScroll(ChangeEvent<float> evt)
        {
            Vector2 trackGroupSize = trackGroup.localBound.size;
            Vector2 viewSize = trackClipArea.localBound.size;
            float range = trackGroupSize.y - viewSize.y;
            if(range < 0)
            {
                verticalSlider.slider.SetValueWithoutNotify(0);
                trackGroup.style.top = 0;
                verticalOffset = 0;
                OnVerticalScrollChanged?.Invoke(0);
                return;
            }
            float y = range * evt.newValue * 0.01f;
            trackGroup.style.top = -y;
            verticalOffset = y;
            OnVerticalScrollChanged?.Invoke(y);
        }

        private void OnScaleChange(bool updateSlider = true)
        {
            timelineTickMarkView.Scale = viewScale;
            trackGroup.UpdateClipPosition();
            if (updateSlider && hasGeometryChange)
            {
                UpdateHorizontalSliderRange();
            }
            OnScaleChanged?.Invoke(viewScale);
        }

        private void UpdateHorizontalSliderRange()
        {
            float trackWidth = GetUnScaleTrackWidth();
            Vector2 viewSize = trackClipArea.localBound.size;
            float x = horizontalOffset / trackWidth * 100f;
            float y = ((viewSize.x / viewScale) + horizontalOffset) / trackWidth * 100f;

            horizontalSlider.SetValueWithoutNotify(new Vector2(x, y));
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            hasGeometryChange = true;
            UpdateHorizontalSliderRange();
        }

        private float FrameToPosition(int frame)
        {
            return ActionLineStyles.FrameInTrackPosition(frame, viewScale) - horizontalOffset * viewScale;
        }
    }
}
