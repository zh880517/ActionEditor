using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Montage
{
    public struct MontageTransitionInfo
    {
        public MontageMotionState From;
        public MontageMotionState To;
        public int FromIndex;
        public int ToIndex;
        public float Weight;
        public float FadeDuration;
        public float FadeTime;
        public AnimationMixerPlayable Mixer;
    }

    [System.Serializable]
    public class MontageRuntime
    {
        [SerializeField]
        private MontageAsset asset;
        private MontageGraph graph;
        private IMontagePlayer player;
        private AnimationMixerPlayable mixerPlayable;
        private AnimationLayerMixerPlayable layerMixerPlayable;
        private readonly List<MontageMotionState> states = new List<MontageMotionState>();
        private readonly List<MontageMotionState> playing = new List<MontageMotionState>();
        private readonly List<MontageTransitionInfo> transitions = new List<MontageTransitionInfo>();

        public IReadOnlyList<MontageMotionState> Playing => playing;
        public IReadOnlyList<MontageTransitionInfo> Transitions => transitions;

        public void Init(MontageAsset asset, MontageGraph graph, IMontagePlayer player)
        {
            this.asset = asset;
            this.graph = graph;
            this.player = player;
            int count = asset.Layers.Count > 0 ? 3 : 2;
            mixerPlayable = AnimationMixerPlayable.Create(graph.Graph, count);
            if (asset.Layers.Count > 0)
            {
                layerMixerPlayable = AnimationLayerMixerPlayable.Create(graph.Graph, asset.Layers.Count);
                for (int i = 0; i < asset.Layers.Count; i++)
                {
                    var layer = asset.Layers[i];
                    layerMixerPlayable.SetLayerAdditive((uint)i, layer.Additive);
                    if (layer.Mask)
                        layerMixerPlayable.SetLayerMaskFromAvatarMask((uint)i, layer.Mask);
                }
                mixerPlayable.ConnectInput(0, layerMixerPlayable, 0);
                mixerPlayable.SetInputWeight(0, 0);
            }
        }

        public void Update(float dt)
        {
            if (playing.Count == 0)
                return;
            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                if(transition.Weight >= 1)
                {
                    transitions.RemoveAt(i);
                    i--;
                    OnTransitionEnd(transition);
                    continue;
                }
                transition.FadeTime += dt;
                transition.Weight = Mathf.Clamp01(transition.FadeTime / transition.FadeDuration);
                transition.Mixer.SetInputWeight(transition.FromIndex, 1 - transition.Weight);
                transition.Mixer.SetInputWeight(transition.ToIndex, transition.Weight);
                transitions[i] = transition;
            }
            for (int i = 0; i < playing.Count; i++)
            {
                var state = playing[i];
                state.Time += dt;
                state.OnUpdate();
            }
        }

        private void OnTransitionEnd(MontageTransitionInfo transition)
        {
        }

        private void OnStatePlayEnd(MontageMotionState state)
        {
        }

        private MontageMotionState GetState(string name)
        {
            var state = states.Find(it => it.Name == name);
            if (state == null)
            {
                var motion = asset.Motions.Find(it => it.name == name);
                if (!motion)
                    return null;
                mixerPlayable.SetInputCount(states.Count + 1);
                state = MontageUtil.CreateState(motion);
                state.Player = player;
                state.DestinationInputPort = states.Count;
                state.Init(graph.Graph);
                state.Connect(mixerPlayable);
                states.Add(state);
            }
            return state;
        }
    }
}
