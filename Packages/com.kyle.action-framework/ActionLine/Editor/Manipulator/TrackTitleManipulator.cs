using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleManipulator : Manipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<TrackTitleMouseDownEvent>(OnMouseDown);
            target.RegisterCallback<TrackTitleMouseEnterEvent>(OnMouseMove);
            target.RegisterCallback<TrackTitleMouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<TrackTitleMouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<TrackTitleMouseEnterEvent>(OnMouseMove);
            target.UnregisterCallback<TrackTitleMouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(TrackTitleMouseDownEvent evt)
        {
        }

        private void OnMouseMove(TrackTitleMouseEnterEvent evt)
        {
        }

        private void OnMouseUp(TrackTitleMouseUpEvent evt)
        {
        }
    }
}
