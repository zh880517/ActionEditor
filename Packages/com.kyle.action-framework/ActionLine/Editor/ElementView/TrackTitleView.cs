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

            icon.style.width = 16;
            icon.style.height = 16;
            Add(icon);
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(titleLabel);
            customArea.style.flexGrow = 1;
            customArea.style.flexDirection = FlexDirection.Row;
            Add(customArea);
            Add(visableButton);
            visableButton.clicked += () => OnVisableClick?.Invoke(this);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
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
            if (custom == element)
                return;
            custom?.RemoveFromHierarchy();
            if (element != null)
                customArea.Add(element);
            custom = element;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            using (var mouseDownEvent = TrackTitleMouseDownEvent.GetPooled(evt.button, Index, evt.mousePosition))
            {
                SendEvent(mouseDownEvent);
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            using (var mouseUpEvent = TrackTitleMouseUpEvent.GetPooled(evt.button, Index, evt.mousePosition))
            {
                SendEvent(mouseUpEvent);
            }
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            using (var mouseMoveEvent = TrackTitleMouseEnterEvent.GetPooled(evt.button, Index, evt.mousePosition))
            {
                SendEvent(mouseMoveEvent);
            }
        }
    }
}
