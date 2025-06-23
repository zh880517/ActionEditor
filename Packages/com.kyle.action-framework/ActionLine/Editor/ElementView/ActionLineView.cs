using UnityEngine.UIElements;
namespace ActionLine.EditorView
{
    public class ActionLineView : VisualElement
    {
        private readonly TrackTitleScrollView trackTitle = new TrackTitleScrollView();
        private readonly TrackScrollView trackScroll = new TrackScrollView();
        private readonly PropertyView property = new PropertyView();
        public TrackTitleScrollView Title => trackTitle;
        public TrackScrollView Track => trackScroll;
        public PropertyView Property => property;

        public float Scale => trackScroll.Scale;
        public float HorizontalOffset => trackScroll.HorizontalOffset;
        public float VerticalOffset => trackScroll.VerticalOffset;

        public ActionLineView()
        {
            var splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            var splitView1 = new TwoPaneSplitView(1, 200, TwoPaneSplitViewOrientation.Horizontal);
            Add(splitView);
            splitView.Add(trackTitle);
            splitView.Add(splitView1);
            splitView.style.flexGrow = 1;
            splitView.style.flexShrink = 1;

            splitView1.Add(trackScroll);
            trackScroll.OnVerticalScrollChanged += (value) =>
            {
                trackTitle.Group.style.top = -value;
            };
            splitView1.Add(property);
        }

        public void SetViewPort(float scale, float horizontalOffset, float verticalOffset)
        {
            trackScroll.SetViewPort(scale, horizontalOffset, verticalOffset);
            trackTitle.style.top = -verticalOffset;
        }
    }
}