using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ActionClipEditorContext
    {
        public ActionClipData Data;
        public ActionClipView ClipView;//由TrackGroupView管理生命周期
        public TrackTitleView TitleView;//由TrackTitleGroupView管理生命周期
        public VisualElement CustomTitle;
        public VisualElement CustomContent;
        public ActionClipEditor Editor;

        public static ActionClipEditorContext CreateEditorContext(ActionClipData clip)
        {
            ActionClipEditorContext context = new ActionClipEditorContext
            {
                Data = clip,
            };
            var types = TypeWithAttributeCollector<ActionClipEditor, CustomClipEditorAttribute>.Types;
            foreach (var kv in types)
            {
                if (kv.Value.ClipEditorType == clip.GetType())
                {
                    context.Editor = (ActionClipEditor)System.Activator.CreateInstance(kv.Key);
                    break;
                }
            }
            context.Editor ??= new ActionClipEditor();
            context.Editor.Clip = clip.Clip;
            context.CustomTitle = context.Editor.CreateCutomTitleElement(clip.Clip);
            context.CustomContent = context.Editor.CreateCustomContentElement(clip.Clip);
            return context;
        }


        public void Destroy()
        {
            CustomTitle?.RemoveFromHierarchy();
            CustomContent?.RemoveFromHierarchy();
        }
    }
}
