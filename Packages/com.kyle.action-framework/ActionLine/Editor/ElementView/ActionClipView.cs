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
                OnMouseDown(evt, -1);
            });
            left.RegisterCallback<MouseMoveEvent>(evt =>
            {
                OnMouseMove(evt, -1);
            });
            left.RegisterCallback<MouseUpEvent>(evt => 
            {
                left.ReleaseMouse();
                OnMouseUp(evt, -1);
            });
            Add(left);
            var right = new MouseCursorRect();
            right.Cursor = MouseCursor.ResizeHorizontal;
            right.AlignParentRight(5);
            right.RegisterCallback<MouseDownEvent>(evt => 
            {
                right.CaptureMouse();
                OnMouseDown(evt, 1);
            });
            right.RegisterCallback<MouseMoveEvent>(evt =>
            {
                OnMouseMove(evt, 1);
            });
            right.RegisterCallback<MouseUpEvent>(evt => 
            {
                right.ReleaseMouse();
                OnMouseUp(evt, 1); 
            });
            Add(right);

            colorElement.pickingMode = PickingMode.Ignore;
            colorElement.AlignParentBottom(4);
            colorElement.style.backgroundColor = Color.white;
            Add(colorElement);

            nameLabel.pickingMode = PickingMode.Ignore;
            nameLabel.StretchToParentSize();
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(nameLabel);

            RegisterCallback<MouseDownEvent>(evt => 
            {
                this.CaptureMouse();
                OnMouseDown(evt, 0);
            });
            RegisterCallback<MouseMoveEvent>(evt =>
            {
                OnMouseMove(evt, 0);
            });
            RegisterCallback<MouseUpEvent>(evt => 
            {
                this.ReleaseMouse();
                OnMouseUp(evt, 0); 
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

        public void SetCustomElement(VisualElement customElement)
        {
            if (customElement != null)
            {
                Add(customElement);
                customElement.StretchToParentSize();
            }
        }

        public void ShowOutLine(bool show)
        {
            if(show)
                this.ShowOutLine(Color.white, 1f);
            else
                this.ShowOutLine(Color.clear, 0f);
        }

        private void OnMouseDown(MouseDownEvent mde, int type)
        {
            using (var evt = ClipMouseDownEvent.GetPooled(mde.button, type, Index, mde.mousePosition, mde.modifiers))
            {
                SendEvent(evt);
            }
        }

        private void OnMouseUp(MouseUpEvent mue, int type)
        {
            using (var evt = ClipMouseUpEvent.GetPooled(mue.button, type, Index, mue.mousePosition, mue.modifiers))
            {
                SendEvent(evt);
            }
        }

        private void OnMouseMove(MouseMoveEvent evt, int type)
        {
            if (evt.pressedButtons == 0)
                return;
            using(var newEvt = ClipMouseMoveEvent.GetPooled(evt.button, type, Index, evt.mousePosition, evt.modifiers))
            {
                SendEvent(newEvt);
            }
        }
    }

}
