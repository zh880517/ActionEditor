using UnityEngine;
using UnityEngine.UIElements;
namespace ActionLine.EditorView
{
    public class ActionLineView : VisualElement
    {
        private readonly TrackTitleScrollView trackTitle = new TrackTitleScrollView();
        private readonly TrackScrollView trackScroll = new TrackScrollView();
        public TrackTitleScrollView Title => trackTitle;
        public TrackScrollView Track => trackScroll;

        public float Scale => trackScroll.Scale;
        public float HorizontalOffset => trackScroll.HorizontalOffset;
        public float VerticalOffset => trackScroll.VerticalOffset;

        public ActionLineView()
        {
            var splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            Add(splitView);
            splitView.Add(trackTitle);
            splitView.Add(trackScroll);
            splitView.style.flexGrow = 1;
            splitView.style.flexShrink = 1;
            trackScroll.OnVerticalScrollChanged += (value) =>
            {
                trackTitle.Group.style.top = -value;
            };
        }

        public void SetViewPort(float scale, float horizontalOffset, float verticalOffset)
        {
            trackScroll.SetViewPort(scale, horizontalOffset, verticalOffset);
            trackTitle.style.top = -verticalOffset;
        }
    }
}