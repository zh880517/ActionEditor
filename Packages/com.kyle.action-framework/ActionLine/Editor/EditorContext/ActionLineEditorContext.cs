using System.Collections.Generic;
using System.Linq;

namespace ActionLine.EditorView
{
    public class ActionLineEditorContext
    {
        private readonly List<ActionClipEditorContext> clipEditors = new List<ActionClipEditorContext>();

        public void Update(ActionLineView view)
        {
            var clips = view.Asset.Clips;
            int clipCount = clips.Count;
            view.Track.Group.SetVisableCount(clipCount);
            view.Title.Group.SetVisableCount(clipCount);
            //删除不存在的（被删除或者Undo）
            for (int i = 0; i < clipEditors.Count; i++)
            {
                var editor = clipEditors[i];
                if(!editor.Clip || !clips.Contains(editor.Clip))
                {
                    editor.Destroy();
                    clipEditors.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                int index = clipEditors.FindIndex(x => x.Clip == clip);
                ActionClipEditorContext context = null;
                if (index < 0)
                {
                    context = CreateEditorContext(clip);
                    clipEditors.Insert(i, context);
                }
                else
                {
                    context = clipEditors[index];
                    if(index != i)
                    {
                        clipEditors.RemoveAt(index);
                        clipEditors.Insert(i, context);
                    }
                }
                UpdateClip(context, i, view);
            }
            view.Track.Group.UpdateClipPosition();
        }

        public void Clear(ActionLineView view)
        {
            foreach (var editor in clipEditors)
            {
                editor.Destroy();
            }
            clipEditors.Clear();
            view.Track.Group.SetVisableCount(0);
            view.Title.Group.SetVisableCount(0);
        }

        private void UpdateClip(ActionClipEditorContext context, int index, ActionLineView view)
        {
            context.TitleView = view.Title.Group.GetTitleView(index);
            context.TitleView.SetCustomElement(context.CustomTitle);
            var clipTypeInfo = ActionClipTypeUtil.GetTypeInfo(context.Clip.GetType());
            context.TitleView.SetStyle(clipTypeInfo.ClipColor, clipTypeInfo.Icon);
            context.TitleView.SetTitle(context.Clip.name);
            context.TitleView.SetVisableButton(!context.Clip.Disable);

            context.ClipView = view.Track.Group.GetClipView(index);
            context.ClipView.SetClipColor(clipTypeInfo.ClipColor);
            context.ClipView.SetClipName(context.Editor.GetClipShowName(context.Clip));

            context.ClipView.StartFrame = context.Clip.StartFrame;
            int endFrame = context.Clip.StartFrame + context.Clip.FrameCount;
            if (context.Clip.FrameCount <= 0)
                endFrame = view.Asset.FrameCount;
            context.ClipView.EndFrame = endFrame;
        }

        private ActionClipEditorContext CreateEditorContext(ActionLineClip clip)
        {
            ActionClipEditorContext context = new ActionClipEditorContext
            {
                Clip = clip,
            };
            var types = TypeWithAttributeCollector<ActionClipEditor, CustomClipEditorAttribute>.Types;
            foreach (var kv in types)
            {
                if(kv.Value.ClipEditorType == clip.GetType())
                {
                    context.Editor = (ActionClipEditor)System.Activator.CreateInstance(kv.Key);
                    break;
                }
            }
            context.Editor ??= new ActionClipEditor();
            context.Editor.Clip = clip;
            context.CustomTitle = context.Editor.CreateCutomTitleElement(clip);
            context.CustomContent = context.Editor.CreateCustomContentElement(clip);
            return context;
        }
    }
}
