using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ClipManipulator : Manipulator
    {
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
        }

        private void OnMouseMove(ClipMouseMoveEvent evt)
        {
        }

        private void OnMouseUp(ClipMouseUpEvent evt)
        {
        }
    }
}
