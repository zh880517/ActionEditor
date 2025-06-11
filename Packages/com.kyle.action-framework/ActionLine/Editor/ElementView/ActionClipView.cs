using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ActionClipView : VisualElement
    {
        public ActionClipView()
        {
            style.flexDirection = FlexDirection.Row;
            style.borderBottomWidth = 4;
            style.borderBottomColor = Color.white;

            var left = new VisualElement();
            left.style.position = Position.Absolute;
            left.style.left = 0;
            left.style.top = 0;
            left.style.bottom = 0;
            left.style.width = 5;
            left.SetCursor(MouseCursor.ResizeHorizontal);

            var right = new VisualElement();
            right.style.position = Position.Absolute;
            right.style.right = 0;
            right.style.top = 0;
            right.style.bottom = 0;
            right.style.width = 5;
            right.SetCursor(MouseCursor.ResizeHorizontal);
        }

        public void SetClipColor(Color color)
        {
            style.backgroundColor = color;
        }
    }

}
