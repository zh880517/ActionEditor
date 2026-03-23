using UnityEngine;
using UnityEngine.UIElements;

namespace Timeline
{
    /// <summary>
    /// Track 拖拽时显示的插入位置提示线
    /// </summary>
    public class InsertIndicatorElement : VisualElement
    {
        public InsertIndicatorElement()
        {
            style.position = Position.Absolute;
            style.height = 2;
            style.left = 0;
            style.right = 0;
            style.backgroundColor = new Color(0.3f, 0.6f, 1f, 1f);
            visible = false;
            pickingMode = PickingMode.Ignore;
        }

        public void Show(float top)
        {
            style.top = top;
            visible = true;
        }

        public void Hide()
        {
            visible = false;
        }
    }
}
