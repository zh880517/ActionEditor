using System;
using System.Collections.Generic;
using PropertyEditor;
using Timeline;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class ClipMotionEditorView : MotionEditorView
    {
        public override MotionType Type => MotionType.Clip;

        // ---- 布局 ----
        private readonly ListView clipListView;
        // ---- Timeline 常量 ----
        private const int FrameRate = 30;
        private const string TrackKey = "ClipTrack";
        private readonly TrackView track;

        // ---- Clip 属性面板 ----
        private readonly StructedPropertyElement clipPropertyEditor = new StructedPropertyElement(typeof(MotionClip), handleUndo: false);

        // ---- ListView 使用的快照列表（与 motion.Clips 相互独立，用于安全 Undo） ----
        private List<MotionClip> clipsSnapshot = new List<MotionClip>();

        public ClipMotionEditorView()
        {
            track = timelineView.AddTrack(TrackKey, TrackFlag.ClipMixable);// 允许融合，禁止拖动
            timelineView.RegisterCallback<ClipSelectEvent>(OnTimelineClipSelect);

            // ---- Clip 列表 ----
            var listHeader = new Label("Clips");
            listHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            listHeader.style.marginTop = 4;
            listHeader.style.marginLeft = 4;
            scrollView.Add(listHeader);

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

            clipListView = new ListView
            {
                makeItem = MakeClipListItem,
                bindItem = BindClipListItem,
                selectionType = SelectionType.Single,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showBorder = true,
                fixedItemHeight = 22,
                style = { flexShrink = 0, minHeight = 66 }
            };
            clipListView.selectedIndicesChanged += OnListSelectionChanged;
            clipListView.itemIndexChanged += OnListItemReordered;
            scrollView.Add(clipListView);

            // ---- Clip 属性面板 ----
            clipPropertyEditor.RegisterCallback<RegisterUndoEvent>(OnClipPropertyChanged);
            clipPropertyEditor.RegisterCallback<PropertyValueChangedEvent>(OnClipPropertyValueChanged);

            var weight = clipPropertyEditor.Find("Weight");
            if (weight != null)
                weight.style.display = DisplayStyle.None;

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
                clipsSnapshot = new List<MotionClip>();
                clipListView.itemsSource = clipsSnapshot;
                clipListView.Rebuild();
                RefreshTimeline();
                RefreshPropertyPanel();
                return;
            }

            // 修正 selectedIndex 范围
            if (selectedIndex >= motion.Clips.Count)
                selectedIndex = motion.Clips.Count - 1;

            RefreshListView();
            RefreshTimeline();
            RefreshPropertyPanel();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ListView
        // ─────────────────────────────────────────────────────────────────────

        private VisualElement MakeClipListItem() => new Label();

        private void BindClipListItem(VisualElement element, int index)
        {
            var clip = (MotionClip)clipListView.itemsSource[index];
            string assetName = clip.Asset != null ? clip.Asset.name : "(no asset)";
            ((Label)element).text = $"{index}  {assetName}";
        }

        private void RefreshListView()
        {
            // itemsSource 始终指向快照，避免 ListView 直接修改 motion.Clips
            clipsSnapshot = motion != null ? new List<MotionClip>(motion.Clips) : new List<MotionClip>();
            clipListView.itemsSource = clipsSnapshot;
            clipListView.Rebuild();

            // 同步选中态（不触发回调）
            if (selectedIndex >= 0 && selectedIndex < (motion?.Clips.Count ?? 0))
            {
                clipListView.SetSelectionWithoutNotify(new[] { selectedIndex });
            }
            else
            {
                clipListView.ClearSelection();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Timeline
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshTimeline()
        {
            track.RemoveAll();

            if (motion == null || motion.Clips.Count == 0)
            {
                timelineView.SetFrameCount(60);
                timelineView.SetCurrentFrame(0);
                return;
            }

            // 计算各 Clip 的起始帧（所有 Clip 相连，没有间隔）
            // 没有 Asset 的 Clip 不在 Timeline 中显示，也不占据帧位置
            int cursor = 0;
            for (int i = 0; i < motion.Clips.Count; i++)
            {
                var clip = motion.Clips[i];
                int len = ClipLengthInFrames(clip);
                if (clip.Asset == null || len <= 0)
                    continue;

                // MixIn：第一个有效 Clip 的 MixIn 不生效
                int mixFrames = cursor == 0 ? 0 : Mathf.RoundToInt(clip.MixIn * clip.GetLength() * FrameRate);
                int startFrame = Mathf.Max(0, cursor - mixFrames);

                track.AddClip(clip.GUID, startFrame, len, GetClipColor(i), clip.Asset.name);
                cursor += (len - mixFrames);
            }

            SetValidFrameCount(cursor);

            // 同步 Timeline 选中态
            SyncTimelineSelection();
        }

        private static int ClipLengthInFrames(MotionClip clip)
        {
            float len = clip.GetLength();
            if (len <= 0) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(len * FrameRate));
        }

        private static Color GetClipColor(int index)
        {
            // 用色相循环为不同索引分配颜色
            float hue = (index * 0.618033988749f) % 1f;
            return Color.HSVToRGB(hue, 0.6f, 0.85f);
        }

        private void SyncTimelineSelection()
        {
            if (motion == null || selectedIndex < 0 || selectedIndex >= motion.Clips.Count)
            {
                track.UnSelectAll();
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
            var mixIn = clipPropertyEditor.Find("MixIn");
            if(mixIn != null)
                mixIn.style.display = selectedIndex == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnClipPropertyChange()
        {
            if (motion == null || clipPropertyEditor == null)
                return;
            if (selectedIndex < 0 || selectedIndex >= motion.Clips.Count)
                return;

            LitAnimEditorUtil.RegisterUndo(motion, "clip property change");
            motion.Clips[selectedIndex] = (MotionClip)clipPropertyEditor.Value;
            motion.OnModify();
            if(selectedIndex > 0)
            {
                var pre = motion.Clips[selectedIndex - 1];
                var select = motion.Clips[selectedIndex];
                if(select.MixIn * select.GetLength() > pre.GetLength())
                {
                    select.MixIn = pre.GetLength() / select.GetLength();
                    motion.Clips[selectedIndex] = select;
                    RefreshPropertyPanel();
                }
            }
            // 刷新 Timeline 和列表（Clip Asset 或时长可能变化）
            RefreshListView();
            RefreshTimeline();
        }

        private void OnClipPropertyValueChanged(PropertyValueChangedEvent evt)
        {
            OnClipPropertyChange();
        }

        private void OnClipPropertyChanged(RegisterUndoEvent evt)
        {
            OnClipPropertyChange();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  事件处理
        // ─────────────────────────────────────────────────────────────────────

        private void OnTimelineClipSelect(ClipSelectEvent evt)
        {
            if (motion == null) return;
            string clipGUID = evt.ClipKey;

            // 找到对应的列表索引
            int idx = motion.Clips.FindIndex(c => c.GUID == clipGUID);
            if (idx < 0 || idx == selectedIndex) return;

            selectedIndex = idx;
            clipListView.SetSelectionWithoutNotify(new[] { selectedIndex });
            RefreshPropertyPanel();
        }

        private void OnListSelectionChanged(IEnumerable<int> selection)
        {
            if (motion == null) return;

            int newIndex = clipListView.selectedIndex;
            if (newIndex == selectedIndex) return;

            selectedIndex = newIndex;
            SyncTimelineSelection();
            RefreshPropertyPanel();
        }

        private void OnListItemReordered(int from, int to)
        {
            if (motion == null) return;

            // 此时 clipsSnapshot 已被 ListView 排好序，而 motion.Clips 尚未改变。
            // 先记录 Undo（记录的是操作前状态），再将快照结果 copy 回源数据。
            LitAnimEditorUtil.RegisterUndo(motion, "Reorder Motion Clips");
            motion.Clips.Clear();
            motion.Clips.AddRange(clipsSnapshot);
            motion.OnModify();

            // 修正选中索引
            if (selectedIndex == from)
                selectedIndex = to;
            else if (from < selectedIndex && to >= selectedIndex)
                selectedIndex--;
            else if (from > selectedIndex && to <= selectedIndex)
                selectedIndex++;

            RefreshListView();
            RefreshTimeline();
            RefreshPropertyPanel();
        }

        private void OnAddClip()
        {
            if (motion == null) return;

            LitAnimEditorUtil.RegisterUndo(motion, "Add Motion Clip");
            var newClip = MotionClip.Default;
            newClip.GUID = Guid.NewGuid().ToString("N");
            motion.Clips.Add(newClip);
            motion.OnModify();

            selectedIndex = motion.Clips.Count - 1;
            RefreshListView();
            RefreshTimeline();
            RefreshPropertyPanel();
        }

        private void OnRemoveClip()
        {
            if (motion == null) return;
            if (selectedIndex < 0 || selectedIndex >= motion.Clips.Count) return;

            LitAnimEditorUtil.RegisterUndo(motion, "Remove Motion Clip");
            motion.Clips.RemoveAt(selectedIndex);
            motion.OnModify();

            if (selectedIndex >= motion.Clips.Count)
                selectedIndex = motion.Clips.Count - 1;

            RefreshListView();
            RefreshTimeline();
            RefreshPropertyPanel();
        }
    }
}
