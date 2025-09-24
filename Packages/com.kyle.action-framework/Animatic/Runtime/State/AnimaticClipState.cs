using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Animatic
{
    public class AnimaticClipState : AnimaticMotionState
    {
        public override AnimaticMotion Motion => motion;
        private readonly AnimaticClip motion;
        private AnimationClipPlayable playable;
        public AnimaticClipState(AnimaticClip motion) : base(motion)
        {
            this.motion = motion;
        }

        public override void Init(PlayableGraph graph)
        {
            playable = AnimationClipPlayable.Create(graph, motion.Asset);
        }

        public override void Connect<V>(V destination)
        {
            destination.ConnectInput(DestinationInputPort, playable, 0);
        }
        public override void OnUpdate()
        {
            if (!playable.IsValid()) return;
            double time = GetStateTime();
            time = time * motion.Speed + motion.StartOffset;
            playable.SetTime(time);
        }

        public override void Destroy()
        {
            if (playable.IsValid())
                playable.Destroy();
        }
    }
}
