using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Animatic
{
    /// <summary>
    /// 多方向融合动画片段，使用时使用0-1的值进行融合控制,计算方式
    /// </summary>
    public class AnimaticBlendClip : AnimaticMotion
    {
        [System.Serializable]
        public struct Motion
        {
            public AnimationClip Asset;
            public float Weight;
        }

        public override float Length
        {
            get
            {
                float maxLength = 0;
                foreach (var motion in Motions)
                {
                    if (motion.Asset)
                    {
                        maxLength = Mathf.Max(maxLength, motion.Asset.length);
                    }
                }
                return maxLength;
            }
        }

        public override bool Valid => Motions.Count(it => it.Asset) > 0;

        public string Param;
        public List<Motion> Motions = new List<Motion>();
    }
}
