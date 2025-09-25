using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Montage
{
    public class MontageSpliceMotionState : MontageMotionState
    {
        public struct ClipTimeInfo
        {
            public double MixInLength;
            public double StartTime;
            public double EndTime;
        }

        public override MontageMotion Motion => motion;
        private readonly MontageSpliceMotion motion;
        private AnimationMixerPlayable mixerPlayable;
        public AnimationClipPlayable[] playables;
        public ClipTimeInfo[] timeInfos;
        public MontageSpliceMotionState(MontageSpliceMotion motion) : base(motion)
        {
            this.motion = motion;
        }

        public override void Init(PlayableGraph graph)
        {
            mixerPlayable = AnimationMixerPlayable.Create(graph, motion.Splices.Count);
            playables = new AnimationClipPlayable[motion.Splices.Count];
            for (int i = 0; i < motion.Splices.Count; i++)
            {
                var splice = motion.Splices[i];
                if (splice.Asset)
                {
                    var playable = AnimationClipPlayable.Create(graph, splice.Asset);
                    playables[i] = playable;
                    playable.ConnectInput(0, mixerPlayable, i);
                }
            }
            timeInfos = new ClipTimeInfo[motion.Splices.Count];
            for (int i = 0; i < motion.Splices.Count; i++)
            {
                var splice = motion.Splices[i];
                var length = splice.GetLength();
                if(i > 1)
                {
                    var pre = timeInfos[i - 1];
                    var mixTime = splice.MixIn * length;
                    mixTime = Mathf.Min((float)mixTime, (float)(pre.EndTime - pre.StartTime));
                    double startTime = pre.EndTime - mixTime;
                    timeInfos[i] = new ClipTimeInfo 
                    {
                        MixInLength = mixTime,
                        StartTime = startTime,
                        EndTime = startTime + length,
                    };
                }
                else
                {
                    timeInfos[0] = new ClipTimeInfo { StartTime = 0, EndTime = length };
                }
            }
        }

        public override void Connect<V>(V destination)
        {
            destination.ConnectInput(DestinationInputPort, mixerPlayable, 0);
        }
        public override void OnUpdate()
        {
            if (!mixerPlayable.IsValid()) return;
            double time = GetStateTime();
            float weight = -1;
            for (int i = 0; i < timeInfos.Length; i++)
            {
                var playable = playables[i];
                if (!playable.IsValid()) continue;
                var info = timeInfos[i];
                var splice = motion.Splices[i];
                if (time >= info.StartTime && time <= info.EndTime)
                {
                    double localTime = time - info.StartTime;
                    localTime = localTime * splice.Speed + splice.StartOffset;
                    playable.SetTime(localTime);
                    if(weight > 0)
                    {
                        //说明需要和上一个片段混合
                        mixerPlayable.SetInputWeight(i, weight);
                        weight = -1;
                    }
                    else if(i + 1 < timeInfos.Length)
                    {
                        //计算和下一个片段的混合权重
                        weight = 0;
                        var next = timeInfos[i + 1];
                        if(time >= next.StartTime && time <= next.EndTime)
                        {
                            //下一个片段需要混合
                            weight = (float)((time - next.StartTime) / next.MixInLength);
                            weight = Mathf.Clamp01(weight);
                        }
                        mixerPlayable.SetInputWeight(i, 1 - weight);
                    }
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
            {
                mixerPlayable.Destroy();
                if (playables != null)
                {
                    foreach (var playable in playables)
                    {
                        if (playable.IsValid())
                            playable.Destroy();
                    }
                }
            }
        }
    }
}
