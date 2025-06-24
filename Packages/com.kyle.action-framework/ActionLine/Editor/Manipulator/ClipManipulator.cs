using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ClipManipulator : Manipulator
    {
        private readonly ActionLineEditorContext context;
        private int startFrame;
        private bool isDragging;
        private bool isStart;
        public ClipManipulator(ActionLineEditorContext editor)
        {
            context = editor;
        }
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<ClipMouseDownEvent>(OnMouseDown);
            target.RegisterCallback<ClipMouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<ClipMouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<ClipMouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<ClipMouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<ClipMouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(ClipMouseDownEvent evt)
        {
            if(evt.Button == 0)
            {
                startFrame = context.View.Track.GetFrameInTrackByMousePosition(evt.MousePosition);
                if(context.SelectedClips.Exists(it=>!it.IsInherit))
                    isStart = true;
            }
        }

        private void OnMouseMove(ClipMouseMoveEvent evt)
        {
            if(isDragging || isStart)
            {
                if(!isDragging)
                {
                    isDragging = true;
                    foreach (var item in context.SelectedClips)
                    {
                        if (!item.IsInherit)
                            continue;
                        context.RegisterUndo("Move Clip", item.Clip);
                    }
                }
                int currentFrame = context.View.Track.GetFrameInTrackByMousePosition(evt.MousePosition);
                context.View.Track.FitFrameInView(currentFrame);
                int offset = currentFrame - startFrame;
                if (offset != 0)
                {
                    startFrame = currentFrame;
                    int maxCount = context.Target.FrameCount;
                    foreach (var item in context.SelectedClips)
                    {
                        if (!item.IsInherit)
                            continue;
                        var clip = item.Clip;
                        int endFrame = clip.StartFrame + clip.Length;
                        if (evt.Type == -1)
                        {
                            //移动并修改长度
                            int newStart = Mathf.Clamp(clip.StartFrame + offset, 0, endFrame - 1);
                            clip.StartFrame = newStart;
                            clip.Length = endFrame - newStart;
                        }
                        else if(evt.Type == 0)
                        {
                            //整体移动
                            int newStart = Mathf.Clamp(clip.StartFrame + offset, 0, endFrame - 1);
                            clip.StartFrame = newStart;
                        }
                        else if(evt.Type == 1)
                        {
                            //修改长度
                            int newEnd = Mathf.Clamp(endFrame + offset, clip.StartFrame + 1, maxCount);
                            clip.Length = newEnd - clip.StartFrame;
                        }
                    }
                }
            }
        }

        private void OnMouseUp(ClipMouseUpEvent evt)
        {
            if(isDragging)
            {
                isDragging = false;
                return;
            }
            if (evt.Button == 0)
            {
                context.SelectClip(evt.ClipIndex, evt.ActionKey);
            }
            else if(evt.Button == 1)
            {
                int currentFrame = context.View.Track.GetFrameInTrackByMousePosition(evt.MousePosition);
                context.ShowContextMenue(ActionModeType.Clip, currentFrame);
            }
        }
    }
}
