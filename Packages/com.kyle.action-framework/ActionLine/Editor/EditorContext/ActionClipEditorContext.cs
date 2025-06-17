using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ActionClipEditorContext
    {
        public ActionLineClip Clip;
        public ActionClipView ClipView;//由TrackGroupView管理生命周期
        public TrackTitleView TitleView;//由TrackTitleGroupView管理生命周期
        public VisualElement CustomTitle;
        public VisualElement CustomContent;
        public ActionClipEditor Editor;

        public void Destroy()
        {
            CustomTitle?.RemoveFromHierarchy();
            CustomContent?.RemoveFromHierarchy();
        }
    }
}
