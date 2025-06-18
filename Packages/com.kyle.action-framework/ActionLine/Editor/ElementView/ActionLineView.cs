using UnityEngine;
using UnityEngine.UIElements;
namespace ActionLine.EditorView
{
    public class ActionLineView : VisualElement
    {
        private readonly TrackTitleScrollView trackTitle = new TrackTitleScrollView();
        private readonly TrackScrollView trackScroll = new TrackScrollView();
        private readonly ActionLineEditorContext editorContext = new ActionLineEditorContext();
        public ActionLineAsset Asset { get; private set; }
        public TrackTitleScrollView Title => trackTitle;
        public TrackScrollView Track=> trackScroll;

        public float Scale => trackScroll.Scale;

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

        public void Update(ActionLineAsset asset)
        {
            if(asset != Asset)
            {
                editorContext.Clear(this);
                Asset = asset;
            }
            editorContext.Update(this);
            trackScroll.SetFrameCount(Asset.FrameCount);
            trackTitle.PlayButtons.MaxFrame = Asset.FrameCount - 1;
        }
    }
}