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
                        //TODO:判断当前位置是否和当前选中轨道位置相同，如果相同则不移动
                        //TODO:移动轨道位置，如果是多选，则按照原有的顺序排序后移动
                    }
                }
                context.View.Title.Group.HideDragLine();
                return;
            }
            if (evt.Button == 0)
            {
                SelectTrack(evt.Index, evt.ActionKey);
            }
            else if (evt.Button == 1)
            {
                context.ShowContextMenue(ActionModeType.TrackTitle);
            }
        }

        private void SelectTrack(int index, bool multi)
        {
            var data = context.Clips[index];
            if (multi)
            {
                int selectedIndex = context.SelectedTracks.IndexOf(data);
                if (selectedIndex >= 0)
                {
                    context.SelectedTracks.RemoveAt(selectedIndex);
                }
                else
                {
                    context.SelectedTracks.Add(data);
                }
            }
            else
            {
                context.SelectedTracks.Clear();
                context.SelectedTracks.Add(data);
            }
        }
    }
}
