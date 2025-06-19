using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleManipulator : Manipulator
    {
        private readonly ActionLineEditorContext context;
        private bool isDragging;
        private bool isStart;
        public TrackTitleManipulator(ActionLineEditorContext editor)
        {
            context = editor;
        }
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<TrackTitleMouseDownEvent>(OnMouseDown);
            target.RegisterCallback<TrackTitleMouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<TrackTitleMouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<TrackTitleMouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<TrackTitleMouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<TrackTitleMouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(TrackTitleMouseDownEvent evt)
        {
            if (evt.Button == 0)
            {
                isStart = true;
            }
        }

        private void OnMouseMove(TrackTitleMouseMoveEvent evt)
        {
            if (isDragging || isStart)
            {
                if (!isDragging)
                {
                    isDragging = true;
                }
                if(context.SelectedTracks.Count > 0)
                {
                    if(!context.SelectedTracks.Exists(it=>it.IsInherit))
                    {
                        int index = context.View.Title.Group.GetTrackIndexByMousePosition(evt.MousePositon);
                        context.View.Title.Group.ShowDragLineAfter(index);
                    }
                }
            }
        }

        private void OnMouseUp(TrackTitleMouseUpEvent evt)
        {
            if(isDragging)
            {
                isDragging = false;
                if (context.SelectedTracks.Count > 0)
                {
                    if (!context.SelectedTracks.Exists(it => it.IsInherit))
                    {
                        int index = context.View.Title.Group.GetTrackIndexByMousePosition(evt.MousePositon);
                        List<int> trackIndexs = new List<int>();
                        foreach (var item in context.SelectedTracks)
                        {
                            if (item.IsInherit)
                                continue;
                            int currentIndex = context.GetIndex(item);
                            if(currentIndex < 0)
                                continue;
                            trackIndexs.Add(currentIndex);
                        }
                        if(trackIndexs.Count > 0)
                        {
                            trackIndexs.Sort();
                            var firstIndex = trackIndexs[0];
                            var lastIndex = trackIndexs[trackIndexs.Count - 1];
                            if(firstIndex > index || lastIndex < index)
                            {
                                context.RegisterUndo("Move Track Index");

                                List<ActionLineClip> clips = new List<ActionLineClip>();
                                foreach (var item in trackIndexs)
                                {
                                    var clip = context.Clips[item];
                                    clips.Add(clip.Clip);
                                }
                                ActionLineClip preClip = null;
                                if(index > 0)
                                {
                                    preClip = context.Clips[index - 1].Clip;
                                }
                                foreach (var item in clips)
                                {
                                    context.Target.MoveToBehind(item, preClip);
                                    preClip = item;
                                }
                                context.RefreshView();
                            }
                        }
                    }
                }
                context.View.Title.Group.HideDragLine();
                return;
            }
            if (evt.Button == 0)
            {
                context.SelectTrack(evt.Index, evt.ActionKey);
            }
            else if (evt.Button == 1)
            {
                context.ShowContextMenue(ActionModeType.TrackTitle);
            }
        }

    }
}
