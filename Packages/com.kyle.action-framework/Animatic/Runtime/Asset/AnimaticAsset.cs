using System.Collections.Generic;
using UnityEngine;
namespace Animatic
{
    [CreateAssetMenu(fileName = "NewAnimaticAsset", menuName = "Animatic/AnimaticAsset")]
    public class AnimaticAsset : CollectableScriptableObject
    {
        public List<AnimaticMotion> Motions = new List<AnimaticMotion>();
    }
}