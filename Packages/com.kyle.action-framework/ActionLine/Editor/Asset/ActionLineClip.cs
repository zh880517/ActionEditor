using UnityEngine;

namespace ActionLine
{
    public class ActionLineClip : ScriptableObject
    {
        [Combined, ReadOnly]
        public ActionLineAsset Owner;

        [Combined, Display("禁用"), PropertyMotion]
        public bool Disable;

        [Combined, Display("描述"), Multiline]
        public string Description = "123\n456";

        [Combined, Display("开始帧"), PropertyMotion]
        public int StartFrame;
        
        [Combined, Display("帧长度"), PropertyMotion]
        public int Length = 1;
    }
}
