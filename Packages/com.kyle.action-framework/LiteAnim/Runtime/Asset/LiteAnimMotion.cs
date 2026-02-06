using System.Collections.Generic;
using UnityEngine;

namespace LiteAnim
{
    public enum MotionWrapMode
    {
        [InspectorName("停留在最后一帧")]
        Clamp = 0,
        [InspectorName("循环播放")]
        Loop = 1,
        [InspectorName("往返播放")]
        PingPong = 2,
    }

    public enum MotionType
    {
        Clip,
        BlendTree,
    }

    [System.Serializable]
    public struct MotionClip
    {
        public AnimationClip Asset;
        public float StartOffset;//ClipMotion使用，播放速度
        public float EndOffset;//ClipMotion使用，播放速度
        public float Speed;//ClipMotion使用，播放速度
        public float Weight;// BlendTree使用，权重值
        [Range(0, 1)]
        public float MixIn;// ClipMotion使用, 与上一个片段的混合时间百分比 混合时间 = MixOut * Length
        public readonly float GetLength()
        {
            if (!Asset) return 0;
            return Mathf.Max(0, Asset.length - StartOffset - EndOffset) / Speed;
        }

        public static readonly MotionClip Default = new MotionClip { Asset = null, StartOffset = 0, EndOffset = 0, Speed = 1, Weight = 1, MixIn = 0 };
    }

    public class LiteAnimMotion : ScriptableObject
    {
        public virtual float Length => 0;
        public virtual bool Valid => true;
        [SerializeField, HideInInspector]
        private int version = 0;
        public int Version => version;
        public MotionType Type = MotionType.Clip;
        public MotionWrapMode WrapMode = MotionWrapMode.Clamp;
        public List<MotionClip> Clips = new List<MotionClip>();
        public string Param;//仅BlendTree使用，BlendTree使用时根据Param参数进行融合控制

        public float GetLength()
        {
            float length = 0;
            if(Type == MotionType.Clip)
            {
                foreach (var clip in Clips)
                {
                    length += clip.GetLength();
                }
            }
            else
            {
                foreach (var clip in Clips)
                {
                    length = Mathf.Max(length, clip.GetLength());
                }
            }
            return length;
        }

        public bool IsValid()
        {
            if(Type == MotionType.Clip)
            {
                return Clips.Exists(it => it.GetLength() <= 0);
            }
            else
            {
                return !Clips.Exists(it => it.Asset);
            }
        }

        public void OnModify()
        {
            version++;
        }
    }
}
