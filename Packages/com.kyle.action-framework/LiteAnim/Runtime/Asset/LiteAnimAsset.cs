using System.Collections.Generic;
using UnityEngine;
namespace LiteAnim
{
    [System.Serializable]
    public struct MotionFadeOverride
    {
        public LiteAnimMotion From;
        public LiteAnimMotion To;
        public float FadeDuration;
    }

    [CreateAssetMenu(fileName = "NewLiteAnimAsset", menuName = "LitAnim/LiteAnimAsset")]
    public class LiteAnimAsset : CollectableScriptableObject
    {
        public float DefaultFadeDuration = 0.17f;
        [HiddenInPropertyEditor]
        public List<LiteAnimMotion> Motions = new List<LiteAnimMotion>();
        public List<LiteAnimLayer> Layers = new List<LiteAnimLayer>();
        public List<MotionFadeOverride> FadeOverrides = new List<MotionFadeOverride>();

        public float GetFadeDuration(LiteAnimMotion from, LiteAnimMotion to)
        {
            for (int i = 0; i < FadeOverrides.Count; i++)
            {
                var o = FadeOverrides[i];
                if (o.From == from && o.To == to)
                    return o.FadeDuration;
            }
            return DefaultFadeDuration;
        }
    }
}