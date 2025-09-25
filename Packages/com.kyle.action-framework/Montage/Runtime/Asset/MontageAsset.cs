using System.Collections.Generic;
using UnityEngine;
namespace Montage
{
    [CreateAssetMenu(fileName = "NewMontageAsset", menuName = "Montage/MontageAsset")]
    public class MontageAsset : CollectableScriptableObject
    {
        public float DefaultFadeDuration = 0.17f;
        public List<MontageMotion> Motions = new List<MontageMotion>();
    }
}