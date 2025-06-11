using UnityEngine;

namespace ActionLine
{
    public class ActionLineClip : ScriptableObject
    {
        [DisplayName("禁用")]
        public bool Disable;
        [DisplayName("描述")]
        public string Description;

        [DisplayName("开始帧")]
        public int StartFrame;
        [DisplayName("帧长度")]
        public int FrameCount = 1;
    }
}
