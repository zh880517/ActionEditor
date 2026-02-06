using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LiteAnim
{
    public class ClipMotionState : MotionState
    {
        private MotionClip clip;
        private AnimationClipPlayable playable;
        public override void Create(PlayableGraph graph)
        {
            for (int i = 0; i < Motion.Clips.Count; i++)
            {
                var clip = Motion.Clips[i];
                if (clip.Asset)
                {
                    this.clip = clip;
                    playable = AnimationClipPlayable.Create(graph, clip.Asset);
                    break;
                }
            }
        }

        public override void Connect(IConnectable destination, int inputPort)
        {
            destination.Connect(playable, inputPort);
        }
        public override void Evaluate(double time)
        {
            if (!playable.IsValid()) return;
            time = time * clip.Speed + clip.StartOffset;
            playable.SetTime(time);
        }

        public override void Destroy()
        {
            if (playable.IsValid())
                playable.Destroy();
            playable = default;
        }

    }
}
