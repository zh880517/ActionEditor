using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Montage
{
    [System.Serializable]
    public class MontageGraph
    {
        public PlayableGraph Graph;
        public Animator Target;
        private readonly Stack<AnimationMixerPlayable> mixerPlayables = new Stack<AnimationMixerPlayable>();//回收池

        public AnimationMixerPlayable GetMixerPlayable()
        {
            if(mixerPlayables.Count == 0)
                return AnimationMixerPlayable.Create(Graph, 2);
            return mixerPlayables.Pop();
        }
        public void RecycleMixerPlayable(AnimationMixerPlayable mixer)
        {
            for (int i = 0; i < mixer.GetInputCount(); i++)
            {
                mixer.DisconnectInput(i);
            }
            mixerPlayables.Push(mixer);
        }
    }
}
