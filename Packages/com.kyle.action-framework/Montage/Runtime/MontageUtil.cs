using UnityEngine.Playables;

namespace Montage
{
    public enum MontageUpdateMode
    {
        Normal,
        UnscaledTime,
        Manual,
    }

    public static class MontageUtil
    {
        public static DirectorUpdateMode ToDirectorMode(MontageUpdateMode mode)
        {
            switch (mode)
            { 
                case MontageUpdateMode.UnscaledTime:
                    return DirectorUpdateMode.UnscaledGameTime;
                case MontageUpdateMode.Manual:
                    return DirectorUpdateMode.Manual;
            }
            return DirectorUpdateMode.GameTime;
        }

        public static MontageMotionState CreateState(MontageMotion motion)
        {
            switch(motion)
            {
                case MontageClipMotion clip:
                    return new MontageClipMotionState(clip);
                case MontageSpliceMotion spliceClip:
                    return new MontageSpliceMotionState(spliceClip);
                case MontageBlendMotion blendClip:
                    return new MontageBlendMotionState(blendClip);

            }
            throw new System.Exception($"AnimaticMotion of type {motion.GetType().Name} is not supported.");
        }
    }
}
