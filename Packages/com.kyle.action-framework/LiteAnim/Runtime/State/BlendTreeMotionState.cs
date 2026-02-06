using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LiteAnim
{
    public class BlendTreeMotionState : MotionState
    {
        private AnimationMixerPlayable mixerPlayable;
        private AnimationClipPlayable[] playables;
        private float[] thresholds;

        public override void Create(PlayableGraph graph)
        {
            mixerPlayable = AnimationMixerPlayable.Create(graph, Motion.Clips.Count);
            playables = new AnimationClipPlayable[Motion.Clips.Count];
            thresholds = new float[Motion.Clips.Count + 1];
            float weightValue = 0;
            for (int i = 0; i < Motion.Clips.Count; i++)
            {
                var clip = Motion.Clips[i];
                if (clip.Asset)
                {
                    var playable = AnimationClipPlayable.Create(graph, clip.Asset);
                    playables[i] = playable;
                    playable.ConnectInput(0, mixerPlayable, i);
                    weightValue += clip.Weight;
                }
            }
            float weight = 0;
            for (int i = 0; i < Motion.Clips.Count; i++)
            {
                var clip = Motion.Clips[i];
                if (clip.Asset)
                {
                    thresholds[i] = weight / weightValue;
                    weight += clip.Weight;
                }
            }
            thresholds[Motion.Clips.Count] = 1;
        }

        public override void Connect(IConnectable destination, int inputPort)
        {
            destination.Connect(mixerPlayable, inputPort);
        }

        public override void Evaluate(double time)
        {
            if (!mixerPlayable.IsValid())
                return;
            float paramValue = 0;
            if (Player != null && !string.IsNullOrEmpty(Motion.Param))
            {
                paramValue = Player.GetParam(Motion.Param);
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
