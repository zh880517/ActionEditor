using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class TrackTitleScrollView : VisualElement
    {
        private readonly PlayButtonsView buttonsView = new PlayButtonsView();
        private readonly VisualElement trackClipArea = new VisualElement();
        private readonly TrackTitleGroupView trackTitleGroup = new TrackTitleGroupView();

        public TrackTitleGroupView Group => trackTitleGroup;
        public PlayButtonsView PlayButtons => buttonsView;
        public TrackTitleScrollView()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;
            style.flexShrink = 1;
            style.overflow = Overflow.Hidden;
            // 添加播放按钮视图
            buttonsView.style.height = ActionLineStyles.TitleBarHeight;
            buttonsView.style.flexGrow = 0;
            buttonsView.style.flexShrink = 0;
            Add(buttonsView);

            // 添加轨道标题组
            trackClipArea.style.overflow = Overflow.Hidden;
            trackClipArea.style.flexGrow = 1;
            trackClipArea.style.flexShrink = 1;
            trackClipArea.Add(trackTitleGroup);
            Add(trackClipArea);
        }
    }
}
