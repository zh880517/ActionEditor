using System.Collections.Generic;
using Timeline;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    /// <summary>
    /// 融合配置详情编辑（右侧面板）：From/To/Duration 属性 + Timeline 预览
    /// </summary>
    public class FadeOverrideDetailView : VisualElement
    {
        private const int FrameRate = 30;
        private const string FromTrackKey = "FromTrack";
        private const string ToTrackKey = "ToTrack";

        private LiteAnimAsset asset;
        private int selectedIndex = -1;

        // ---- 属性编辑 ----
        private readonly PopupField<string> fromField;
        private readonly PopupField<string> toField;
        private readonly FloatField fadeDurationField;

        // ---- Timeline 预览 ----
        private readonly PlayButtonsView playButtons;
        private readonly ScrollView scrollView;
        private readonly TimelineView timelineView;
        private readonly TrackView fromTrack;
        private readonly TrackView toTrack;

        // ---- Motion 名称缓存 ----
        private List<string> motionNames = new List<string> { "(None)" };

        public FadeOverrideDetailView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;
            style.flexDirection = FlexDirection.Column;

            // ==== 属性编辑 ====
            fromField = new PopupField<string>("From", new List<string> { "(None)" }, 0);
            fromField.RegisterValueChangedCallback(OnFromChanged);
            Add(fromField);

            toField = new PopupField<string>("To", new List<string> { "(None)" }, 0);
            toField.RegisterValueChangedCallback(OnToChanged);
            Add(toField);

            fadeDurationField = new FloatField("Fade Duration");
            fadeDurationField.RegisterValueChangedCallback(OnFadeDurationChanged);
            fadeDurationField.isDelayed = true;
            Add(fadeDurationField);

            // ==== Timeline 预览 ====
            scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;

            playButtons = new PlayButtonsView();
            playButtons.style.height = 20;
            playButtons.style.flexGrow = 0;
            playButtons.style.flexShrink = 0;
            scrollView.Add(playButtons);

            timelineView = new TimelineView();
            timelineView.style.flexShrink = 0;
            timelineView.style.marginBottom = 4;
            timelineView.AutoHeight = true;
            scrollView.Add(timelineView);

            fromTrack = timelineView.AddTrack(FromTrackKey, TrackFlag.None);
            toTrack = timelineView.AddTrack(ToTrackKey, TrackFlag.None);
            Add(scrollView);

            RegisterCallback<FrameIndexChangeEvent>(OnFrameIndexChange);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  公共接口
        // ─────────────────────────────────────────────────────────────────────

        public void RefreshView(LiteAnimAsset asset, int selectedIndex)
        {
            this.asset = asset;
            this.selectedIndex = selectedIndex;
            RebuildMotionNames();

            bool hasTarget = asset != null && selectedIndex >= 0 && selectedIndex < asset.FadeOverrides.Count;

            fromField.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            toField.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            fadeDurationField.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            scrollView.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasTarget)
            {
                fromTrack.RemoveAll();
                toTrack.RemoveAll();
                return;
            }

            RefreshDetail();
            RefreshTimeline();
        }

        public bool TryGetSelected(out MotionFadeOverride selected)
        {
            if (asset != null && selectedIndex >= 0 && selectedIndex < asset.FadeOverrides.Count)
            {
                selected = asset.FadeOverrides[selectedIndex];
                return true;
            }
            selected = default;
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Motion 名称
        // ─────────────────────────────────────────────────────────────────────

        private void RebuildMotionNames()
        {
            motionNames.Clear();
            motionNames.Add("(None)");
            if (asset != null)
            {
                foreach (var m in asset.Motions)
                    motionNames.Add(m != null ? m.name : "<Missing>");
            }
        }

        private string MotionToName(LiteAnimMotion motion)
        {
            if (motion == null) return "(None)";
            return motion.name;
        }

        private LiteAnimMotion NameToMotion(string name)
        {
            if (asset == null || name == "(None)") return null;
            return asset.Motions.Find(m => m != null && m.name == name);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  属性编辑
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshDetail()
        {
            var item = asset.FadeOverrides[selectedIndex];
            fromField.choices = motionNames;
            toField.choices = motionNames;
            fromField.SetValueWithoutNotify(MotionToName(item.From));
            toField.SetValueWithoutNotify(MotionToName(item.To));
            fadeDurationField.SetValueWithoutNotify(item.FadeDuration);
        }

        private void OnFromChanged(ChangeEvent<string> evt)
        {
            if (asset == null || selectedIndex < 0 || selectedIndex >= asset.FadeOverrides.Count) return;
            LitAnimEditorUtil.RegisterUndo(asset, "Edit Fade Override From");
            var item = asset.FadeOverrides[selectedIndex];
            item.From = NameToMotion(evt.newValue);
            asset.FadeOverrides[selectedIndex] = item;
            ViewRefeshEvent.Dispatch(this);
        }

        private void OnToChanged(ChangeEvent<string> evt)
        {
            if (asset == null || selectedIndex < 0 || selectedIndex >= asset.FadeOverrides.Count) return;
            LitAnimEditorUtil.RegisterUndo(asset, "Edit Fade Override To");
            var item = asset.FadeOverrides[selectedIndex];
            item.To = NameToMotion(evt.newValue);
            asset.FadeOverrides[selectedIndex] = item;
            ViewRefeshEvent.Dispatch(this);
        }

        private void OnFadeDurationChanged(ChangeEvent<float> evt)
        {
            if (asset == null || selectedIndex < 0 || selectedIndex >= asset.FadeOverrides.Count) return;
            float duration = Mathf.Max(0f, evt.newValue);
            LitAnimEditorUtil.RegisterUndo(asset, "Edit Fade Duration");
            var item = asset.FadeOverrides[selectedIndex];
            item.FadeDuration = duration;
            asset.FadeOverrides[selectedIndex] = item;
            fadeDurationField.SetValueWithoutNotify(duration);
            ViewRefeshEvent.Dispatch(this);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Timeline 预览
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshTimeline()
        {
            fromTrack.RemoveAll();
            toTrack.RemoveAll();

            if (asset == null || selectedIndex < 0 || selectedIndex >= asset.FadeOverrides.Count)
            {
                timelineView.SetFrameCount(60);
                playButtons.SetMaxFrame(60);
                return;
            }

            var item = asset.FadeOverrides[selectedIndex];
            float fromLen = item.From != null ? item.From.GetLength() : 0;
            float toLen = item.To != null ? item.To.GetLength() : 0;
            float fadeDur = item.FadeDuration;
            int fadeFrames = Mathf.Max(1, Mathf.RoundToInt(fadeDur * FrameRate));
            int fromFrames = Mathf.Max(1, Mathf.RoundToInt(fromLen * FrameRate));
            int toFrames = Mathf.Max(1, Mathf.RoundToInt(toLen * FrameRate));

            // From 在 frame 0 开始；To 在 fromFrames - fadeFrames 开始（交叉融合）
            int toStart = Mathf.Max(0, fromFrames - fadeFrames);
            int totalFrames = Mathf.Max(fromFrames, toStart + toFrames);

            if (item.From != null && fromLen > 0)
                fromTrack.AddClip("from", 0, fromFrames, new Color(0.3f, 0.6f, 0.9f), item.From.name);

            if (item.To != null && toLen > 0)
                toTrack.AddClip("to", toStart, toFrames, new Color(0.9f, 0.5f, 0.3f), item.To.name);

            timelineView.SetFrameCount(totalFrames);
            playButtons.SetMaxFrame(totalFrames);
            timelineView.SetCurrentFrame(0);
            playButtons.SetFrame(0, false);
        }

        private void OnFrameIndexChange(FrameIndexChangeEvent evt)
        {
            timelineView.SetCurrentFrame(evt.Frame);
            playButtons.SetFrame(evt.Frame, false);
        }
    }
}
