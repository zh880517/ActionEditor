using UnityEngine;

namespace ActionLine.EditorView
{
    public static class ActionLineStyles
    {
        public const int FrameWidth = 10;
        public const int ClipHeight = 30;
        public const int TrackInterval = 5;// 轨道上下间隔
        public const int TrackHeaderInterval = 10;// 轨道头部预留
        public const int TrackTailInterval = 10;// 轨道尾部预留
        public static readonly Color GrayBackGroundColor = new Color(70 / 255f, 70 / 255f, 70 / 255f, 0.5f);
        public static readonly Color SelectBackGroundColor = new Color(61 / 255f, 94 / 255f, 152 / 255f, 0.5f);
        public static readonly Color TrackGray = new Color(65 / 255f, 65 / 255f, 65 / 255f, 0.5f);
        public static readonly Color TrackBlue = new Color(61 / 255f, 94 / 255f, 152 / 255f, 0.5f);

        public static float FrameToPosition(int frame, float scale = 1)
        {
            return frame * FrameWidth * scale;
        }

        public static float FrameInTrackPosition(int frame, float scale = 1)
        {
            return (frame * FrameWidth * scale) + TrackHeaderInterval;
        }
    }
}
