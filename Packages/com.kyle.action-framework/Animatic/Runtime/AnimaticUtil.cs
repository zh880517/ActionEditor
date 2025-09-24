using UnityEngine.Playables;

namespace Animatic
{
    public enum AnimaticUpdateMode
    {
        Normal,
        UnscaledTime,
        Manual,
    }

    public static class AnimaticUtil
    {
        public static DirectorUpdateMode ToDirectorMode(AnimaticUpdateMode mode)
        {
            switch (mode)
            { 
                case AnimaticUpdateMode.UnscaledTime:
                    return DirectorUpdateMode.UnscaledGameTime;
                case AnimaticUpdateMode.Manual:
                    return DirectorUpdateMode.Manual;
            }
            return DirectorUpdateMode.GameTime;
        }

        public static AnimaticMotionState CreateState(AnimaticMotion motion)
        {
            switch(motion)
            {
                case AnimaticClip clip:
                    return new AnimaticClipState(clip);
                case AnimaticSpliceClip spliceClip:
                    return new AnimaticSpliceClipState(spliceClip);
                case AnimaticBlendClip blendClip:
                    return new AnimaticBlendClipState(blendClip);

            }
            throw new System.Exception($"AnimaticMotion of type {motion.GetType().Name} is not supported.");
        }
    }
}
