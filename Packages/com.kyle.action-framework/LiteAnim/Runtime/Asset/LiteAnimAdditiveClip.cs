using UnityEngine;

namespace LiteAnim
{
    [System.Serializable]
    public class LiteAnimAdditiveClip
    {
        public string Name;
        public string Layer;//所属layer，同一个Layer同一时间只能有一个AdditiveClip生效
        public string Description;
        public AnimationClip Clip;
    }
}
