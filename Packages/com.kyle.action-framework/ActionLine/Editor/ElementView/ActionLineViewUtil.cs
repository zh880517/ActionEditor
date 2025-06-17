using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionLine.EditorView
{
    public static class ActionLineViewUtil
    {
        public static void UpdateTrackTitleView(TrackTitleView trackTitleView, ActionLineClip bindClip)
        {
            var clipTypeInfo = ActionClipTypeUtil.GetTypeInfo(bindClip.GetType());
            trackTitleView.SetStyle(clipTypeInfo.ClipColor, clipTypeInfo.Icon);
            trackTitleView.SetTitle(bindClip.name);
            trackTitleView.SetVisableButton(!bindClip.Disable);
        }
    }
}
