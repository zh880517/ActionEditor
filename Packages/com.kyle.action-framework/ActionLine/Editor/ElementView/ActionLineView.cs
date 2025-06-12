using UnityEngine.UIElements;
namespace ActionLine.EditorView
{
    public class ActionLineView : VisualElement
    {
        private readonly TwoPaneSplitView splitView;
        public ActionLineView()
        {
            splitView = new TwoPaneSplitView(0, 500, TwoPaneSplitViewOrientation.Horizontal);
            Add(splitView);
            var left = new VisualElement();
            Add(left);
            var right = new VisualElement();
            Add(right);
        }
    }
}