using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleView : VisualElement
    {
        private readonly Image icon = new Image();
        private readonly Label titleLabel = new Label();
        private readonly VisualElement customArea = new VisualElement();
        private VisualElement custom;
        private readonly IconButton visableButton = new IconButton();
        public System.Action<TrackTitleView> OnVisableClick;
        public int Index;

        public TrackTitleView()
        {
            style.flexDirection = FlexDirection.Row;
            style.borderLeftWidth = 5;
            style.borderLeftColor = Color.white;

            icon.pickingMode = PickingMode.Ignore;
            icon.style.width = 16;
            icon.style.height = 16;
            Add(icon);
            titleLabel.pickingMode = PickingMode.Ignore;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(titleLabel);
            custom.pickingMode = PickingMode.Ignore;
            customArea.style.flexGrow = 1;
            customArea.style.flexDirection = FlexDirection.Row;
            Add(customArea);
            Add(visableButton);
            visableButton.clicked += () => OnVisableClick?.Invoke(this);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        public void SetStyle(Color color, Texture iconImg)
        {
            icon.image = iconImg;
            style.borderLeftColor = color;
        }

        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }

        public void SetVisableButton(bool isVisable)
        {
            if (isVisable)
            {
                visableButton.SetBuildinIcon("animationvisibilitytoggleon");
            }
            else
            {
                visableButton.SetBuildinIcon("animationvisibilitytoggleoff");
            }
        }

        public void SetCustomElement(VisualElement element)
        {
            if (element != null)
                customArea.Add(element);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                this.CaptureMouse();
            }
            using (var mouseDownEvent = TrackTitleMouseDownEvent.GetPooled(evt.button, Index, evt.mousePosition, evt.modifiers))
            {
                SendEvent(mouseDownEvent);
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            this.ReleaseMouse();
            using (var mouseUpEvent = TrackTitleMouseUpEvent.GetPooled(evt.button, Index, evt.mousePosition, evt.modifiers))
            {
                SendEvent(mouseUpEvent);
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (evt.pressedButtons == 0)
                return;
            using (var mouseMoveEvent = TrackTitleMouseMoveEvent.GetPooled(evt.button, Index, evt.mousePosition, evt.modifiers))
            {
                SendEvent(mouseMoveEvent);
            }
        }

    }
}
