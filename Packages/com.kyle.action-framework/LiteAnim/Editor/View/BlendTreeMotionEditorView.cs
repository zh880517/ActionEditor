using System;
using System.Collections.Generic;
using PropertyEditor;
using Timeline;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class BlendTreeMotionEditorView : MotionEditorView
    {
        public override MotionType Type => MotionType.BlendTree;

        private const int FrameRate = 30;

        // ---- Clip 属性面板 ----
        private readonly StructedPropertyElement clipPropertyEditor = new StructedPropertyElement(typeof(MotionClip), expandedInParent: true, handleUndo: false);

        // Track key → Clip index（每次 Refresh 重建）
        private readonly List<string> trackKeys = new List<string>();

        public BlendTreeMotionEditorView() : base(trackDragable: true)
        {
            timelineView.style.height = 120;

            timelineView.RegisterCallback<ClipSelectEvent>(OnTimelineClipSelect);
            timelineView.RegisterCallback<TrackIndexChangedEvent>(OnTrackOrderChanged);

            // ---- 工具栏 ----
            var listToolbar = new VisualElement();
            listToolbar.style.flexDirection = FlexDirection.Row;
            listToolbar.style.justifyContent = Justify.FlexEnd;
            var addBtn = new Button(OnAddClip) { text = "+" };
            addBtn.style.width = 24;
            var removeBtn = new Button(OnRemoveClip) { text = "-" };
            removeBtn.style.width = 24;
            listToolbar.Add(addBtn);
            listToolbar.Add(removeBtn);
            scrollView.Add(listToolbar);

            // ---- Clip 属性面板 ----
            clipPropertyEditor.RegisterCallback<RegisterUndoEvent>(OnClipPropertyChanged);
            clipPropertyEditor.RegisterCallback<PropertyValueChangedEvent>(OnClipPropertyValueChanged);
            clipPropertyEditor.SetFieldVisible("StartOffset", false);
            clipPropertyEditor.SetFieldVisible("EndOffset", false);
            clipPropertyEditor.SetFieldVisible("Speed", false);
            clipPropertyEditor.SetFieldVisible("MixIn", false);
            scrollView.Add(clipPropertyEditor);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  公共刷新入口
        // ─────────────────────────────────────────────────────────────────────

        public override void Refresh(LiteAnimMotion motion)
        {
            this.motion = motion;
            if (motion == null)
            {
                selectedIndex = -1;
                RefreshTimeline();
                RefreshPropertyPanel();
                return;
            }

            if (selectedIndex >= motion.Clips.Count)
                selectedIndex = motion.Clips.Count - 1;

            RefreshTimeline();
            RefreshPropertyPanel();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Timeline：每个 Clip 一个 Track，禁止 Clip 移动和融合
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshTimeline()
        {
            // 移除所有旧 Track
            for(int i = trackKeys.Count - 1; i >= 0; i--)
            {
                var key = trackKeys[i];
                if(motion == null || !motion.Clips.Exists(it=>it.GUID == key))
                {
                    timelineView.RemoveTrack(key);
                    trackKeys.RemoveAt(i);
                }
            }
            if (motion == null || motion.Clips.Count == 0)
            {
                timelineView.SetFrameCount(60);
                timelineView.SetCurrentFrame(0);
                return;
            }

            int defaultLen = ComputeDefaultClipFrameLength();

            for (int i = 0; i < motion.Clips.Count; i++)
            {
                var clip = motion.Clips[i];
                string key = clip.GUID;
                // TrackFlag.None：不允许 Clip 移动，不允许融合
                var track = timelineView.AddTrack(key, TrackFlag.None);
                if(!trackKeys.Contains(key))
                    trackKeys.Add(key);

                // 同步显示顺序与 motion.Clips 顺序一致
                timelineView.SetTrackIndex(key, i);

                int len = clip.Asset != null ? ClipLengthInFrames(clip) : defaultLen;
                if (len <= 0) len = defaultLen;

                string label = clip.Asset != null ? clip.Asset.name : "(no asset)";
                label = $"{label} ({clip.Weight})";
                track.AddClip(clip.GUID, 0, len, GetClipColor(i), label);
            }

            timelineView.SetFrameCount(Mathf.Max(60, ComputeMaxFrameLength() + 10));
            SyncTimelineSelection();
        }

        // 计算当前所有有 Asset 的 Clip 中最长的帧数；若都没有则返回 1 秒对应帧数
        private int ComputeDefaultClipFrameLength()
        {
            if (motion == null) return FrameRate;
            int max = 0;
            foreach (var clip in motion.Clips)
            {
                if (clip.Asset == null) continue;
                int len = ClipLengthInFrames(clip);
                if (len > max) max = len;
            }
            return max > 0 ? max : FrameRate;
        }

        private int ComputeMaxFrameLength()
        {
            int defaultLen = ComputeDefaultClipFrameLength();
            int max = 0;
            foreach (var clip in motion.Clips)
            {
                int len = clip.Asset != null ? ClipLengthInFrames(clip) : defaultLen;
                if (len > max) max = len;
            }
            return max;
        }

        private static int ClipLengthInFrames(MotionClip clip)
        {
            float len = clip.GetLength();
            if (len <= 0) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(len * FrameRate));
        }

        private static Color GetClipColor(int index)
        {
            float hue = (index * 0.618033988749f) % 1f;
            return Color.HSVToRGB(hue, 0.6f, 0.85f);
        }

        private void SyncTimelineSelection()
        {
            if (motion == null || selectedIndex < 0 || selectedIndex >= motion.Clips.Count)
            {
                foreach (var key in trackKeys)
                    timelineView.SelectClip(null);
                return;
            }
            timelineView.SelectClip(motion.Clips[selectedIndex].GUID);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  属性面板
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshPropertyPanel()
        {
            if (motion == null || selectedIndex < 0 || selectedIndex >= motion.Clips.Count)
            {
                clipPropertyEditor.style.display = DisplayStyle.None;
                return;
            }

            clipPropertyEditor.style.display = DisplayStyle.Flex;
            clipPropertyEditor.SetValue(motion.Clips[selectedIndex]);
        }

        private void OnClipPropertyChange()
        {
            if (motion == null || clipPropertyEditor == null) return;
            if (selectedIndex < 0 || selectedIndex >= motion.Clips.Count) return;

            LitAnimEditorUtil.RegisterUndo(motion, "clip property change");
            motion.Clips[selectedIndex] = (MotionClip)clipPropertyEditor.Value;
            motion.OnModify();

            RefreshTimeline();
        }

        private void OnClipPropertyValueChanged(PropertyValueChangedEvent evt) => OnClipPropertyChange();
        private void OnClipPropertyChanged(RegisterUndoEvent evt) => OnClipPropertyChange();

        // ─────────────────────────────────────────────────────────────────────
        //  事件处理
        // ─────────────────────────────────────────────────────────────────────

        private void OnTimelineClipSelect(ClipSelectEvent evt)
        {
            if (motion == null) return;
            int idx = motion.Clips.FindIndex(c => c.GUID == evt.ClipKey);
            if (idx < 0 || idx == selectedIndex) return;

            selectedIndex = idx;
            RefreshPropertyPanel();
        }

        // Track 拖拽重排后同步 motion.Clips 顺序
        private void OnTrackOrderChanged(TrackIndexChangedEvent evt)
        {
            if (motion == null) return;

            // evt.OrderedKeys 是按新顺序排列的 Track Key（= Clip GUID）
            var newClips = new List<MotionClip>(motion.Clips.Count);
            string selectedGUID = selectedIndex >= 0 && selectedIndex < motion.Clips.Count
                ? motion.Clips[selectedIndex].GUID : null;

            foreach (string key in evt.OrderedKeys)
            {
                int idx = motion.Clips.FindIndex(c => c.GUID == key);
                if (idx >= 0)
                    newClips.Add(motion.Clips[idx]);
            }

            if (newClips.Count != motion.Clips.Count) return;

            LitAnimEditorUtil.RegisterUndo(motion, "Reorder Blend Clips");
            motion.Clips.Clear();
            motion.Clips.AddRange(newClips);
            motion.OnModify();

            // 修正 selectedIndex
            if (selectedGUID != null)
                selectedIndex = motion.Clips.FindIndex(c => c.GUID == selectedGUID);

            // trackKeys 已在 RefreshTimeline 中重建，此处直接刷新
            RefreshTimeline();
            RefreshPropertyPanel();
        }

        private void OnAddClip()
        {
            if (motion == null) return;

            LitAnimEditorUtil.RegisterUndo(motion, "Add Blend Clip");
            var newClip = MotionClip.Default;
            newClip.GUID = Guid.NewGuid().ToString("N");
            motion.Clips.Add(newClip);
            motion.OnModify();

            selectedIndex = motion.Clips.Count - 1;
            RefreshTimeline();
            RefreshPropertyPanel();
        }

        private void OnRemoveClip()
        {
            if (motion == null) return;
            if (selectedIndex < 0 || selectedIndex >= motion.Clips.Count) return;

            LitAnimEditorUtil.RegisterUndo(motion, "Remove Blend Clip");
            motion.Clips.RemoveAt(selectedIndex);
            motion.OnModify();

            if (selectedIndex >= motion.Clips.Count)
                selectedIndex = motion.Clips.Count - 1;

            RefreshTimeline();
            RefreshPropertyPanel();
        }
    }
}
