using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ActionClipView : VisualElement
    {
        private readonly VisualElement colorElement = new VisualElement();
        private readonly Label nameLabel = new Label();
        public ActionClipView()
        {
            style.backgroundColor = ActionLineStyles.GrayBackGroundColor;
            var left = new MouseCursorRect();
            left.Cursor = MouseCursor.ResizeHorizontal;
            left.AlignParentLeft(5);
            Add(left);
            var right = new MouseCursorRect();
            right.Cursor = MouseCursor.ResizeHorizontal;
            right.AlignParentRight(5);
            Add(right);

            colorElement.AlignParentBottom(4);
            colorElement.style.backgroundColor = Color.white;
            Add(colorElement);

            nameLabel.StretchToParentSize();
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(nameLabel);
        }

        public void SetClipColor(Color color)
        {
            colorElement.style.backgroundColor = color;
        }

        public void SetClipName(string name)
        {
            nameLabel.text = name;
        }

        public void ShowOutLine(bool show)
        {
            if(show)
                this.ShowOutLine(Color.white, 1f);
            else
                this.ShowOutLine(Color.clear, 0f);
        }

    }

}
