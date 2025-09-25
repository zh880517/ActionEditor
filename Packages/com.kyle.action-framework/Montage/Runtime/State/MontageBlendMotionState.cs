using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Montage
{
    public class MontageBlendMotionState : MontageMotionState
    {
        public override MontageMotion Motion => motion;
        private readonly MontageBlendMotion motion;
        private AnimationMixerPlayable mixerPlayable;
        private AnimationClipPlayable[] playables;
        private float[] thresholds;

        public MontageBlendMotionState(MontageBlendMotion motion) : base(motion)
        {
            this.motion = motion;
        }


        public override void Init(PlayableGraph graph)
        {
            mixerPlayable = AnimationMixerPlayable.Create(graph, motion.Motions.Count);
            playables = new AnimationClipPlayable[motion.Motions.Count];
            thresholds = new float[motion.Motions.Count + 1];
            float weightValue = 0;
            for (int i = 0; i < motion.Motions.Count; i++)
            {
                var clip = motion.Motions[i];
                if (clip.Asset)
                {
                    var playable = AnimationClipPlayable.Create(graph, clip.Asset);
                    playables[i] = playable;
                    playable.ConnectInput(0, mixerPlayable, i);
                    weightValue += clip.Weight;
                }
            }
            float weight = 0;
            for (int i = 0; i < motion.Motions.Count; i++)
            {
                var clip = motion.Motions[i];
                if (clip.Asset)
                {
                    thresholds[i] = weight / weightValue;
                    weight += clip.Weight;
                }
            }
            thresholds[motion.Motions.Count] = 1;
        }

        public override void Connect<V>(V destination)
        {
            destination.ConnectInput(DestinationInputPort, mixerPlayable, 0);
        }

        public override void OnUpdate()
        {
            if (!mixerPlayable.IsValid())
                return;
            double time = GetStateTime();
            float paramValue = 0;
            if (Player != null && !string.IsNullOrEmpty(motion.Param))
            {
                paramValue = Player.GetParam(motion.Param);
                paramValue = Mathf.Clamp01(paramValue);
            }
            int preIndex = -1;
            int nextIndex = -1;
            float preWeight = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if(paramValue < thresholds[i])
                {
                    preIndex = i - 1;
                    nextIndex = i % playables.Length;
                    preWeight = (paramValue - thresholds[preIndex]) / (thresholds[nextIndex] - thresholds[preIndex]);
                }
            }
            for (int i = 0; i < playables.Length; i++)
            {
                var playable = playables[i];
                if (!playable.IsValid()) continue;
                playable.SetTime(time);
                if (i == preIndex)
                {
                    mixerPlayable.SetInputWeight(i, preWeight);
                }
                else if (i == nextIndex)
                {
                    mixerPlayable.SetInputWeight(i, 1 - preWeight);
                }
                else
                {
                    mixerPlayable.SetInputWeight(i, 0);
                }
            }
        }
        public override void Destroy()
        {
            if (mixerPlayable.IsValid())
                mixerPlayable.Destroy();
            foreach (var playable in playables)
            {
                if (playable.IsValid())
                    playable.Destroy();
            }
        }

    }
}
