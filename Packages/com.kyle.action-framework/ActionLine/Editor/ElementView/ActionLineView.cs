using UnityEngine.UIElements;
namespace ActionLine.EditorView
{
    public class ActionLineView : VisualElement
    {
        private readonly PlayButtonsView buttonsView = new PlayButtonsView();

        public ActionLineView()
        {
            var splitView = new TwoPaneSplitView(0, 500, TwoPaneSplitViewOrientation.Horizontal);
            Add(splitView);
            var left = new VisualElement();
            Add(left);


            var right = new VisualElement();
            Add(right);
        }
    }
}