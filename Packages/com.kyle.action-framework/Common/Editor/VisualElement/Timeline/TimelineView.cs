using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timeline
{
    public class TimelineView : VisualElement
    {
        public float FrameWidth { get; set; } = 10f;
        public float TitleBarHeight { get; set; } = 20f;
        public float HeaderInterval { get; set; } = 10f;
        public float TrackTailInterval { get; set; } = 50f;

        // Clip 拖拽结束时触发：(clipKey, 帧偏移量)
        public System.Action<string, int> OnClipMoved;

        private readonly TickMarkView tickMarkView = new TickMarkView();
        private readonly CursorView cursorView = new CursorView();
        private readonly VisualElement trackClipArea = new VisualElement();
        private readonly VisualElement trackContainer = new VisualElement();
        private readonly MinMaxSlider horizontalSlider = new MinMaxSlider();
        private readonly Scroller verticalSlider;

        private readonly List<TrackView> tracks = new List<TrackView>();
        private ClipView currentSelected;

        private float scale = 1f;
        private float horizontalOffset;
        private float verticalOffset;
        private int frameCount;
        private bool hasGeometry;

        public int CurrentFrame => cursorView.CurrentFrame;

        public TimelineView()
        {
            style.overflow = Overflow.Hidden;
            style.flexGrow = 1;

            // 垂直滚动条 — 右侧边缘
            verticalSlider = new Scroller(0, 100, null, SliderDirection.Vertical);
            verticalSlider.style.position = Position.Absolute;
            verticalSlider.style.right = 0;
            verticalSlider.style.top = 0;
            verticalSlider.style.bottom = 20;
            verticalSlider.style.width = 20;
            verticalSlider.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);
            Add(verticalSlider);

            // 水平滑块 — 底部边缘
            horizontalSlider.style.position = Position.Absolute;
            horizontalSlider.style.bottom = 0;
            horizontalSlider.style.left = 0;
            horizontalSlider.style.right = 20;
            horizontalSlider.style.height = 20;
            horizontalSlider.value = new Vector2(0, 100);
            horizontalSlider.lowLimit = 0;
            horizontalSlider.highLimit = 100;
            horizontalSlider.RegisterValueChangedCallback(OnHorizontalScroll);
            Add(horizontalSlider);

            // 中央区域 — 占满除滚动条槽以外的空间
            var center = new VisualElement();
            center.style.position = Position.Absolute;
            center.style.left = 0;
            center.style.right = 20;
            center.style.top = 0;
            center.style.bottom = 20;
            center.style.overflow = Overflow.Hidden;
            center.RegisterCallback<WheelEvent>(OnWheel);
            center.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Add(center);

            // TickMarkView — 铺满中央区域，绘制刻度并响应点击设置帧指针
            tickMarkView.StretchToParentSize();
            tickMarkView.TitleHeight = TitleBarHeight;
            tickMarkView.HeaderInterval = HeaderInterval;
            tickMarkView.FrameWidth = FrameWidth;
            tickMarkView.SetCursorView(cursorView);
            center.Add(tickMarkView);

            // 轨道 Clip 区域 — 位于刻度栏下方
            trackClipArea.style.position = Position.Absolute;
            trackClipArea.style.left = 0;
            trackClipArea.style.right = 0;
            trackClipArea.style.top = TitleBarHeight;
            trackClipArea.style.bottom = 0;
            trackClipArea.style.overflow = Overflow.Hidden;
            center.Add(trackClipArea);

            // 轨道容器 — 纵向排列 TrackView，支持垂直滚动
            trackContainer.style.flexDirection = FlexDirection.Column;
            trackContainer.style.position = Position.Absolute;
            trackContainer.style.left = 0;
            trackContainer.style.right = 0;
            trackContainer.style.top = 0;
            trackContainer.pickingMode = PickingMode.Ignore;
            trackClipArea.Add(trackContainer);

            // CursorView — 最后添加，渲染在所有 Clip 上方
            cursorView.StretchToParentSize();
            cursorView.TitleHeight = TitleBarHeight;
            center.Add(cursorView);

            // 监听从轨道冒泡上来的事件
            RegisterCallback<ClipMoveEvent>(OnClipMove);
            RegisterCallback<ClipSelectEvent>(OnClipSelect);
        }

        public TrackView AddTrack(TrackFlag flags)
        {
            var track = new TrackView(flags);
            track.style.left = 0;
            track.style.right = 0;
            tracks.Add(track);
            trackContainer.Add(track);
            if (hasGeometry)
                UpdateVerticalSliderRange();
            return track;
        }

        public void RemoveTrack(int index)
        {
            if (index < 0 || index >= tracks.Count)
                return;
            tracks[index].RemoveFromHierarchy();
            tracks.RemoveAt(index);
            if (hasGeometry)
                UpdateVerticalSliderRange();
        }

        public void SetFrameCount(int count)
        {
            frameCount = count;
            tickMarkView.FrameCount = count;
            cursorView.FrameCount = count;
            trackContainer.style.width = count * FrameWidth * scale + HeaderInterval + TrackTailInterval;
            if (hasGeometry)
                UpdateHorizontalSliderRange();
        }

        public void SetCurrentFrame(int frame)
        {
            cursorView.CurrentFrame = frame;
        }

        public void FitFrameInView(int frame)
        {
            if (!hasGeometry)
                return;
            float unscaled = frame * FrameWidth;
            float viewWidth = trackClipArea.localBound.size.x;
            float viewUnscaled = viewWidth / scale;
            float offset = Mathf.Min(40f, viewWidth * 0.1f) / scale;
            if (unscaled >= horizontalOffset && unscaled < horizontalOffset + viewUnscaled)
                return;
            float trackTotal = GetUnscaledTrackWidth();
            float x;
            if (unscaled < horizontalOffset)
                x = Mathf.Max(0f, (unscaled - offset) / trackTotal * 100f);
            else
                x = Mathf.Max(0f, (unscaled - viewUnscaled + offset) / trackTotal * 100f);
            var prev = horizontalSlider.value;
            horizontalSlider.value = new Vector2(x, x + (prev.y - prev.x));
        }

        private void OnClipSelect(ClipSelectEvent evt)
        {
            // 遍历所有轨道按 key 查找 ClipView
            ClipView clip = null;
            foreach (var track in tracks)
            {
                clip = track.FindClip(evt.ClipKey);
                if (clip != null) break;
            }
            if (clip == null || currentSelected == clip)
            {
                evt.StopPropagation();
                return;
            }
            currentSelected?.SetSelected(false);
            currentSelected = clip;
            clip.SetSelected(true);
            evt.StopPropagation();
        }

        private void OnClipMove(ClipMoveEvent evt)
        {
            OnClipMoved?.Invoke(evt.ClipKey, evt.OffsetFrame);
            evt.StopPropagation();
        }

        private void OnWheel(WheelEvent evt)
        {
            scale = Mathf.Clamp(scale - Mathf.Sign(evt.delta.y) * 0.1f, 0.1f, 10f);
            ApplyScaleChange();
        }

        private void OnHorizontalScroll(ChangeEvent<Vector2> evt)
        {
            float trackWidth = GetUnscaledTrackWidth();
            float viewWidth = trackClipArea.localBound.size.x;
            float viewRate = viewWidth / trackWidth;
            float newRate = (evt.newValue.y - evt.newValue.x) * 0.01f;
            if (newRate <= 0)
            {
                horizontalSlider.SetValueWithoutNotify(evt.previousValue);
                return;
            }
            float newScale = viewRate / newRate;
            if (newScale < 0.1f || newScale > 10f)
            {
                horizontalSlider.SetValueWithoutNotify(evt.previousValue);
                return;
            }
            scale = newScale;
            horizontalOffset = evt.newValue.x * 0.01f * trackWidth;
            tickMarkView.HorizontalOffset = horizontalOffset;
            ApplyScaleChange(updateSlider: false);
        }

        private void OnVerticalScroll(ChangeEvent<float> evt)
        {
            float containerH = trackContainer.localBound.size.y;
            float areaH = trackClipArea.localBound.size.y;
            float range = containerH - areaH;
            if (range <= 0)
            {
                trackContainer.style.top = 0;
                verticalOffset = 0;
                return;
            }
            verticalOffset = range * evt.newValue * 0.01f;
            trackContainer.style.top = -verticalOffset;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            hasGeometry = true;
            UpdateHorizontalSliderRange();
            UpdateVerticalSliderRange();
        }

        private void ApplyScaleChange(bool updateSlider = true)
        {
            tickMarkView.Scale = scale;
            foreach (var track in tracks)
                track.UpdateClipPositions(scale, horizontalOffset, FrameWidth, HeaderInterval);
            trackContainer.style.width = frameCount * FrameWidth * scale + HeaderInterval + TrackTailInterval;
            if (updateSlider && hasGeometry)
                UpdateHorizontalSliderRange();
        }

        private void UpdateHorizontalSliderRange()
        {
            float trackWidth = GetUnscaledTrackWidth();
            if (trackWidth <= 0)
                return;
            float viewWidth = trackClipArea.localBound.size.x;
            float x = horizontalOffset / trackWidth * 100f;
            float y = (horizontalOffset + viewWidth / scale) / trackWidth * 100f;
            horizontalSlider.SetValueWithoutNotify(new Vector2(x, Mathf.Min(100f, y)));
        }

        private void UpdateVerticalSliderRange()
        {
            float containerH = trackContainer.localBound.size.y;
            float areaH = trackClipArea.localBound.size.y;
            if (containerH <= areaH)
            {
                verticalSlider.slider.SetValueWithoutNotify(0);
                return;
            }
            float ratio = verticalOffset / (containerH - areaH) * 100f;
            verticalSlider.slider.SetValueWithoutNotify(ratio);
        }

        private float GetUnscaledTrackWidth()
        {
            return frameCount * FrameWidth + HeaderInterval + TrackTailInterval;
        }
    }
}
