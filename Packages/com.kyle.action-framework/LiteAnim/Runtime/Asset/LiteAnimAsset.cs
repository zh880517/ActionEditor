using System.Collections.Generic;
using UnityEngine;
namespace LiteAnim
{
    [CreateAssetMenu(fileName = "NewLiteAnimAsset", menuName = "LitAnim/LiteAnimAsset")]
    public class LiteAnimAsset : CollectableScriptableObject
    {
        public float DefaultFadeDuration = 0.17f;
        [HiddenInPropertyEditor]
        public List<LiteAnimMotion> Motions = new List<LiteAnimMotion>();
        public List<LiteAnimLayer> Layers = new List<LiteAnimLayer>();
    }
}