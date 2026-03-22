using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timeline
{
    [Flags]
    public enum TrackFlag
    {
        None = 0,
        ClipMixable = 1 << 0,
        ClipMovable = 1 << 1,
        DisableClipPool = 1 << 2
    }

    public class TrackView : VisualElement
    {
        public TrackFlag Flags { get; private set; }
        public bool ClipMixable => (Flags & TrackFlag.ClipMixable) != 0;
        public bool ClipMovable => (Flags & TrackFlag.ClipMovable) != 0;
        public bool DisableClipPool => (Flags & TrackFlag.DisableClipPool) != 0;

        // 始终按 StartFrame 升序维护
        private readonly List<ClipView> clips = new List<ClipView>();

        // ClipView 回收池
        private readonly Stack<ClipView> clipPool = new Stack<ClipView>();
        private float scale = 1f;
        private float horizontalOffset;
        private float frameWidth = 10f;
        private float headerInterval = 10f;

        // 选中状态
        private string selectedClipKey;

        // 拖拽状态
        private bool isDragging;
        private string dragClipKey;
        private float dragStartMouseX;
        private int dragAccumulatedDelta;

        private const float DragThreshold = 3f;

        // 重叠可视化覆盖层（单一元素，绘制所有重叠区域）
        private readonly OverlapDrawElement overlapOverlay;

        public TrackView(TrackFlag flags)
        {
            Flags = flags;
            style.height = 40f;
            style.flexShrink = 0;
            style.paddingTop = 5f;
            style.paddingBottom = 5f;
            style.backgroundColor = new Color(65 / 255f, 65 / 255f, 65 / 255f, 0.5f);
            style.overflow = Overflow.Hidden;
            style.marginBottom = 2f;

            // 重叠覆盖层渲染在所有 Clip 上方
            overlapOverlay = new OverlapDrawElement();
            Add(overlapOverlay);

            // TrackView 统一处理所有鼠标事件
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        public void AddClip(string key, int startFrame, int length, Color color, string name)
        {
            var existing = FindClip(key);
            if (existing != null)
            {
                // key 已存在：刷新参数并重新排序
                existing.StartFrame = startFrame;
                existing.Length = length;
                UpdateClipPositions(scale, horizontalOffset, frameWidth, headerInterval);
                ResortClip(existing);
                return;
            }
            var clip = (!DisableClipPool && clipPool.Count > 0) ? clipPool.Pop() : new ClipView();
            clip.Init(key, startFrame, length, color, name);
            clip.style.display = DisplayStyle.Flex;
            InsertClipSorted(clip);
            UpdateClipPositions(scale, horizontalOffset, frameWidth, headerInterval);
        }

        public void RemoveClip(string key)
        {
            int idx = FindClipIndex(key);
            if (idx < 0) return;
            var clip = clips[idx];
            if (DisableClipPool)
            {
                clip.RemoveFromHierarchy();
            }
            else
            {
                clip.style.display = DisplayStyle.None;
                clipPool.Push(clip);
            }
            clips.RemoveAt(idx);
            if (ClipMixable)
                UpdateOverlaps();
        }

        public void RemoveAll()
        {
            foreach (var clip in clips)
            {
                if (DisableClipPool)
                    clip.RemoveFromHierarchy();
                else
                {
                    clip.style.display = DisplayStyle.None;
                    clipPool.Push(clip);
                }
            }
            clips.Clear();
            selectedClipKey = null;
            if (ClipMixable)
                UpdateOverlaps();
        }

        public void UnSelectAll()
        {
            foreach (var clip in clips)
                clip.SetSelected(false);
            if (selectedClipKey != null)
            {
                selectedClipKey = null;
                if (ClipMixable)
                    UpdateOverlaps();
            }
        }

        // x = frame * frameWidth * scale + headerInterval - horizontalOffset * scale
        public void UpdateClipPositions(float scale, float hOffset, float fw, float hi)
        {
            this.scale = scale;
            horizontalOffset = hOffset;
            frameWidth = fw;
            headerInterval = hi;
            float ppf = fw * scale;
            foreach (var clip in clips)
            {
                float left = clip.StartFrame * ppf + hi - hOffset * scale;
                float width = clip.Length * ppf;
                clip.UpdateLayout(left, width);
            }
            if (ClipMixable)
                UpdateOverlaps();
        }

        #region Clip 列表辅助方法

        public ClipView FindClip(string key)
        {
            foreach (var c in clips)
                if (c.Key == key) return c;
            return null;
        }

        private int FindClipIndex(string key)
        {
            for (int i = 0; i < clips.Count; i++)
                if (clips[i].Key == key) return i;
            return -1;
        }

        // 将 Clip 插入有序列表，并同步 VisualElement 层级顺序
        private void InsertClipSorted(ClipView clip)
        {
            int insertAt = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clip.StartFrame < clips[i].StartFrame)
                {
                    insertAt = i;
                    break;
                }
            }
            clips.Insert(insertAt, clip);

            // 同步到 VisualElement 层级，确保在 overlapOverlay 下方
            if (insertAt == clips.Count - 1)
                clip.PlaceBehind(overlapOverlay);
            else
                clip.PlaceBehind(clips[insertAt + 1]);
        }

        // StartFrame 变化后将 Clip 移动到列表中正确的位置
        private void ResortClip(ClipView clip)
        {
            int oldIdx = clips.IndexOf(clip);
            if (oldIdx < 0) return;

            // 找到新插入位置（跳过当前位置）
            int newIdx = 0;
            for (int i = 0; i < clips.Count; i++)
            {
                if (i == oldIdx) continue;
                if (clips[i].StartFrame <= clip.StartFrame)
                    newIdx = i + 1;
                else
                    break;
            }
            // 补偿移除后的索引偏移
            if (newIdx > oldIdx) newIdx--;

            if (newIdx == oldIdx) return;

            clips.RemoveAt(oldIdx);
            clips.Insert(newIdx, clip);

            // 同步 VisualElement 层级顺序
            if (newIdx == clips.Count - 1)
                clip.PlaceBehind(overlapOverlay);
            else
                clip.PlaceBehind(clips[newIdx + 1]);
        }

        #endregion

        #region 命中测试

        // 返回 (localX, localY) 处命中的 Clip key，未命中返回 null。
        // 可混合轨道的重叠区域由对角线分割：
        //   线上方（y/h < t）→ 前一个 Clip（StartFrame 较小）
        //   线下方（y/h >= t）→ 后一个 Clip（StartFrame 较大）
        //   其中 t = (localX - overlapLeft) / (overlapRight - overlapLeft)
        private string HitTest(float localX, float localY)
        {
            if (!ClipMixable)
            {
                foreach (var clip in clips)
                {
                    float clipLeft = FrameToLocalX(clip.StartFrame);
                    float clipRight = FrameToLocalX(clip.StartFrame + clip.Length);
                    if (localX >= clipLeft && localX <= clipRight)
                        return clip.Key;
                }
                return null;
            }

            // clips 已按 StartFrame 升序排列
            float h = localBound.height;

            for (int i = 0; i < clips.Count; i++)
            {
                int iStart = clips[i].StartFrame;
                int iEnd = iStart + clips[i].Length;
                for (int j = i + 1; j < clips.Count; j++)
                {
                    int jStart = clips[j].StartFrame;
                    if (jStart >= iEnd) break;

                    int overlapStart = Mathf.Max(iStart, jStart);
                    int overlapEnd = Mathf.Min(iEnd, jStart + clips[j].Length);

                    float left = FrameToLocalX(overlapStart);
                    float right = FrameToLocalX(overlapEnd);

                    if (localX >= left && localX <= right && right > left)
                    {
                        // t：重叠区域内归一化 x 位置 [0,1]
                        // 对角线方向：左上（前一个）→ 右下（后一个）
                        float t = (localX - left) / (right - left);
                        float normalizedY = h > 0 ? localY / h : 0f;
                        return normalizedY < t ? clips[i].Key : clips[j].Key;
                    }
                }
            }

            // 未命中重叠区域，退回普通命中测试
            foreach (var clip in clips)
            {
                float clipLeft = FrameToLocalX(clip.StartFrame);
                float clipRight = FrameToLocalX(clip.StartFrame + clip.Length);
                if (localX >= clipLeft && localX <= clipRight)
                    return clip.Key;
            }
            return null;
        }

        #endregion

        #region 鼠标事件

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
                return;

            string key = HitTest(evt.localMousePosition.x, evt.localMousePosition.y);
            if (key == null)
                return;

            // 无论是否可移动都触发选中
            if (selectedClipKey != key)
            {
                selectedClipKey = key;
                if (ClipMixable)
                    UpdateOverlaps();
            }
            using var selectEvt = ClipSelectEvent.GetPooled(this, key);
            SendEvent(selectEvt);

            if (!ClipMovable)
            {
                evt.StopPropagation();
                return;
            }

            dragClipKey = key;
            dragStartMouseX = evt.mousePosition.x;
            dragAccumulatedDelta = 0;
            isDragging = false;

            this.CaptureMouse();
            evt.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (dragClipKey == null || (evt.pressedButtons & 1) == 0)
                return;

            float pixelPerFrame = frameWidth * scale;
            if (pixelPerFrame <= 0) return;

            float dx = evt.mousePosition.x - dragStartMouseX;
            if (!isDragging && Mathf.Abs(dx) < DragThreshold)
                return;
            isDragging = true;

            int delta = Mathf.RoundToInt(dx / pixelPerFrame);
            if (delta == 0) return;
            dragStartMouseX += delta * pixelPerFrame;
            dragAccumulatedDelta += delta;

            ApplyClipDrag(dragClipKey, delta);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0)
                return;

            this.ReleaseMouse();

            if (isDragging && dragClipKey != null && dragAccumulatedDelta != 0)
            {
                // 拖拽结束时发送一次 ClipMoveEvent，携带累计帧偏移
                using var moveEvt = ClipMoveEvent.GetPooled(this, dragClipKey, dragAccumulatedDelta);
                SendEvent(moveEvt);
            }

            isDragging = false;
            dragClipKey = null;
            dragAccumulatedDelta = 0;
        }

        #endregion

        #region Clip 拖拽逻辑

        private void ApplyClipDrag(string key, int delta)
        {
            var clip = FindClip(key);
            if (clip == null) return;

            clip.StartFrame = Mathf.Max(0, clip.StartFrame + delta);

            if (!ClipMixable)
                ClampNonMixableOverlap(clip, delta);

            ResortClip(clip);
            UpdateClipPositions(scale, horizontalOffset, frameWidth, headerInterval);
        }

        #endregion

        #region 重叠处理

        // 将帧号转换为 TrackView 本地像素坐标 x
        private float FrameToLocalX(int frame)
        {
            return frame * frameWidth * scale + headerInterval - horizontalOffset * scale;
        }

        private void UpdateOverlaps()
        {
            overlapOverlay.ClearAll();

            // clips 已按 StartFrame 升序排列
            for (int i = 0; i < clips.Count; i++)
            {
                int iStart = clips[i].StartFrame;
                int iEnd = iStart + clips[i].Length;
                for (int j = i + 1; j < clips.Count; j++)
                {
                    int jStart = clips[j].StartFrame;
                    if (jStart >= iEnd) break;
                    int jEnd = jStart + clips[j].Length;

                    int overlapStart = Mathf.Max(iStart, jStart);
                    int overlapEnd = Mathf.Min(iEnd, jEnd);

                    float left = FrameToLocalX(overlapStart);
                    float right = FrameToLocalX(overlapEnd);

                    // clips[i] 为前一个，clips[j] 为后一个
                    OverlapSelection selection = OverlapSelection.None;
                    if (selectedClipKey == clips[i].Key)
                        selection = OverlapSelection.PrevSelected;
                    else if (selectedClipKey == clips[j].Key)
                        selection = OverlapSelection.NextSelected;

                    overlapOverlay.AddZone(new OverlapZone(left, right, selection));
                }
            }
        }

        // 限制被移动的 Clip，使其不能进入其他 Clip 的帧范围
        private void ClampNonMixableOverlap(ClipView moved, int delta)
        {
            foreach (var other in clips)
            {
                if (other == moved) continue;
                int oe = other.StartFrame;
                int oe2 = oe + other.Length;
                int me2 = moved.StartFrame + moved.Length;
                if (moved.StartFrame < oe2 && me2 > oe)
                {
                    moved.StartFrame = delta > 0 ? oe - moved.Length : oe2;
                    moved.StartFrame = Mathf.Max(0, moved.StartFrame);
                }
            }
        }

        #endregion
    }
}
