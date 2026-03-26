using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LiteAnim
{
    public class BlendTreeMotionState : MotionState
    {
        private AnimationMixerPlayable mixerPlayable;
        private AnimationClipPlayable[] playables;
        private float[] centers; // 各 Clip 在圆上的中心位置 [0, 1)

        public override void Create(PlayableGraph graph)
        {
            int count = Motion.Clips.Count;
            mixerPlayable = AnimationMixerPlayable.Create(graph, count);
            playables = new AnimationClipPlayable[count];
            centers = new float[count];
            float totalWeight = 0;
            for (int i = 0; i < count; i++)
            {
                var clip = Motion.Clips[i];
                if (clip.Asset)
                {
                    var playable = AnimationClipPlayable.Create(graph, clip.Asset);
                    playables[i] = playable;
                    playable.ConnectInput(0, mixerPlayable, i);
                    totalWeight += clip.Weight;
                }
            }
            if (totalWeight <= 0) totalWeight = 1;

            // 圆形布局：Clip 0 中心位于 0，其余按权重比例顺次排列
            float halfFirst = Motion.Clips[0].Weight * 0.5f;
            float cumWeight = 0;
            for (int i = 0; i < count; i++)
            {
                centers[i] = (cumWeight + Motion.Clips[i].Weight * 0.5f - halfFirst) / totalWeight;
                cumWeight += Motion.Clips[i].Weight;
            }
        }

        public override void Connect(IConnectable destination, int inputPort)
        {
            destination.Connect(mixerPlayable, inputPort);
        }

        public override void Connect<V>(V playable, int index)
        {
            playable.ConnectInput(index, mixerPlayable, 0);
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

            int count = playables.Length;

            // 在圆上找 paramValue 落在哪两个相邻 center 之间
            int prevIdx = count - 1;
            int nextIdx = 0;
            float prevCenter = centers[count - 1];
            float nextCenter = centers[0] + 1f; // 环绕：第一个 Clip 在圆的另一端

            for (int i = 0; i < count; i++)
            {
                if (paramValue < centers[i])
                {
                    nextIdx = i;
                    nextCenter = centers[i];
                    prevIdx = (i - 1 + count) % count;
                    prevCenter = i > 0 ? centers[i - 1] : centers[count - 1] - 1f;
                    break;
                }
            }

            float range = nextCenter - prevCenter;
            float blend = range > 0 ? (paramValue - prevCenter) / range : 0;
            blend = Mathf.Clamp01(blend);

            for (int i = 0; i < count; i++)
            {
                var playable = playables[i];
                if (!playable.IsValid()) continue;
                playable.SetTime(time);

                float w = 0;
                if (prevIdx == nextIdx)
                    w = 1f;
                else if (i == prevIdx)
                    w = 1f - blend;
                else if (i == nextIdx)
                    w = blend;
                mixerPlayable.SetInputWeight(i, w);
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
