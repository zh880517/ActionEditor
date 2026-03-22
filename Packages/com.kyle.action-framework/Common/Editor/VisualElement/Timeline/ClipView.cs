using UnityEngine;
using UnityEngine.UIElements;

namespace Timeline
{
    public class ClipView : VisualElement
    {
        public string Key { get; private set;}
        public int StartFrame { get; set; }
        public int Length { get; set; }

        private readonly VisualElement colorBar;
        private readonly Label nameLabel;

        public ClipView(string key, int startFrame, int length, Color color, string name)
        {
            Key = key;
            StartFrame = startFrame;
            Length = length;

            pickingMode = PickingMode.Ignore;

            style.position = Position.Absolute;
            style.top = 0;
            style.bottom = 0;
            style.overflow = Overflow.Hidden;
            style.backgroundColor = color;

            // 底部色彩装饰条
            colorBar = new VisualElement();
            colorBar.AlignParentBottom(4);
            colorBar.pickingMode = PickingMode.Ignore;
            colorBar.style.backgroundColor = Color.white;
            Add(colorBar);

            // 居中名称标签
            nameLabel = new Label(name);
            nameLabel.StretchToParentSize();
            nameLabel.pickingMode = PickingMode.Ignore;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(nameLabel);
        }

        public void UpdateLayout(float left, float width)
        {
            style.left = left;
            style.width = width;
        }

        public void SetSelected(bool selected)
        {
            this.ShowOutLine(selected ? Color.white : Color.clear, selected ? 1f : 0f);
        }
    }
}
