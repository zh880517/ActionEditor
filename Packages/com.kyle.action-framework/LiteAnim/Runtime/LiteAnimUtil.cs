using UnityEngine.Playables;

namespace LiteAnim
{
    public enum MontageUpdateMode
    {
        Normal,
        UnscaledTime,
        Manual,
    }

    public static class LiteAnimUtil
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

        public static MotionState CreateState(LiteAnimMotion motion)
        {
            MotionState state = null;
            switch (motion.Type)
            {
                case MotionType.Clip:
                    state = new ClipMotionState();
                    break;
                case MotionType.BlendTree:
                    state = new BlendTreeMotionState();
                    break;

            }
            if(state != null)
            {
                state.Init(motion);
                return state;
            }
            throw new System.Exception($"LiteAnimMotion of type ({motion.Type}) is not supported.");
        }
    }
}
