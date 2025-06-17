using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ActionClipView : VisualElement
    {
        private readonly VisualElement colorElement = new VisualElement();
        private readonly Label nameLabel = new Label();
        public int Index;
        public int StartFrame;
        public int EndFrame;
        public ActionClipView()
        {
            style.backgroundColor = ActionLineStyles.GrayBackGroundColor;
            var left = new MouseCursorRect();
            left.Cursor = MouseCursor.ResizeHorizontal;
            left.AlignParentLeft(5);
            left.RegisterCallback<MouseDownEvent>(evt => 
            {
                left.CaptureMouse();
                OnMouseDown(evt.button, -1);
            });
            left.RegisterCallback<MouseUpEvent>(evt => 
            {
                left.ReleaseMouse();
                OnMouseUp(evt.button, -1);
            });
            Add(left);
            var right = new MouseCursorRect();
            right.Cursor = MouseCursor.ResizeHorizontal;
            right.AlignParentRight(5);
            right.RegisterCallback<MouseDownEvent>(evt => 
            {
                right.CaptureMouse();
                OnMouseDown(evt.button, 1);
            });
            right.RegisterCallback<MouseUpEvent>(evt => 
            {
                right.ReleaseMouse();
                OnMouseUp(evt.button, 1); 
            });
            Add(right);

            colorElement.AlignParentBottom(4);
            colorElement.style.backgroundColor = Color.white;
            Add(colorElement);

            nameLabel.StretchToParentSize();
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(nameLabel);

            RegisterCallback<MouseDownEvent>(evt => 
            {
                this.CaptureMouse();
                OnMouseDown(evt.button, 0);
            });
            RegisterCallback<MouseUpEvent>(evt => 
            {
                this.ReleaseMouse();
                OnMouseUp(evt.button, 0); 
            });
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

        private void OnMouseDown(int button, int type)
        {
            //Clip的拖拽事件由TimelineTickMarkView的OnDragFrame响应，此处只处理鼠标按下事件
            //通知上层View Clip被点击
        }

        private void OnMouseUp(int button, int type)
        {
            //通知上层View Clip被释放
        }
    }

}
