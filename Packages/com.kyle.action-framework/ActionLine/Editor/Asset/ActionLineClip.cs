using UnityEngine;

namespace ActionLine
{
    public class ActionLineClip : ScriptableObject
    {
        #region 编辑用临时属性，不保存
        public bool Foldout { get; set; } = true;
        #endregion
        [Combined, ReadOnly]
        public ActionLineAsset Owner;

        [Combined, Display("禁用"), PropertyMotion]
        public bool Disable;

        [Combined, Display("描述"), Multiline]
        public string Description;

        [Combined, Display("开始帧"), PropertyMotion]
        public int StartFrame;
        
        [Combined, Display("帧长度"), PropertyMotion]
        public int Length = 1;
    }
}
