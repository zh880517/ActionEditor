using UnityEngine;

namespace Montage
{
    [System.Serializable]
    public class MontageLayer
    {
        public string LayerName;
        public bool Additive;
        public AvatarMask Mask;
    }
}
