using System.Collections.Generic;
using UnityEngine;

namespace Animatic
{
    public class AnimaticSpliceClip : AnimaticMotion
    {
        [System.Serializable]
        public struct Splice
        {
            public AnimationClip Asset;
            public float StartOffset;
            public float EndOffset;
            public float Speed;
            [Range(0, 1)]
            public float MixIn;// 与上一个片段的混合时间百分比 混合时间 = MixOut * Length
            public readonly float GetLength()
            {
                if (!Asset) return 0;
                return Mathf.Max(0, Asset.length - StartOffset - EndOffset) / Speed;
            }

            public static Splice Create(AnimationClip asset)
            {
                return new Splice()
                {
                    Asset = asset,
                    StartOffset = 0,
                    EndOffset = 0,
                    Speed = 1,
                    MixIn = 0,
                };
            }
        }

        public override float Length
        {
            get
            {
                float total = 0;
                foreach (var splice in Splices)
                {
                    total += splice.GetLength();
                }
                return total;
            }
        }

        public override bool Valid => !Splices.Exists(it => it.GetLength() <= 0);

        public List<Splice> Splices = new List<Splice>();
    }
}
