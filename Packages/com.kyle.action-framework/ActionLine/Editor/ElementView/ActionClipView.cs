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
            var left = new VisualElement();
            left.AlignParentLeft(5);
            left.SetCursor(MouseCursor.ResizeHorizontal);
            Add(left);

            var right = new VisualElement();
            right.style.position = Position.Absolute;
            right.AlignParentRight(5);
            right.SetCursor(MouseCursor.ResizeHorizontal);
            Add(right);

            colorElement.AlignParentBottom(4);
            colorElement.style.backgroundColor = Color.white;
            colorElement.pickingMode = PickingMode.Ignore;
            Add(colorElement);

            nameLabel.StretchToParentSize();
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.pickingMode = PickingMode.Ignore;
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
