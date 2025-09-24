using UnityEngine;

namespace Animatic
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

    public class AnimaticMotion : ScriptableObject
    {
        public virtual float Length => 0;
        public MotionWrapMode WrapMode = MotionWrapMode.Clamp;
        public virtual bool Valid => true;
    }
}
