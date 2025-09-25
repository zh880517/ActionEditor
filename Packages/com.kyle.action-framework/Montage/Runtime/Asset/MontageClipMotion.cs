using UnityEngine;

namespace Montage
{
    public class MontageClipMotion : MontageMotion
    {
        public override float Length
        {
            get
            {
                if (!Asset) return 0;
                return Mathf.Max(0, Asset.length - StartOffset - EndOffset)/Speed;
            }
        }

        public override bool Valid => Asset && Length > 0;

        public AnimationClip Asset;
        public float StartOffset = 0;
        public float EndOffset = 0;
        public float Speed = 1;
    }
}
