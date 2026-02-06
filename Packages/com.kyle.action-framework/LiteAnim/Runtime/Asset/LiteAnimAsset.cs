using System.Collections.Generic;
using UnityEngine;
namespace LiteAnim
{
    [CreateAssetMenu(fileName = "NewMontageAsset", menuName = "Montage/MontageAsset")]
    public class LiteAnimAsset : CollectableScriptableObject
    {
        public float DefaultFadeDuration = 0.17f;
        public List<LiteAnimMotion> Motions = new List<LiteAnimMotion>();
        public List<LiteAnimLayer> Layers = new List<LiteAnimLayer>();
        public List<LiteAnimAdditiveClip> AdditiveClips = new List<LiteAnimAdditiveClip>();
    }
}