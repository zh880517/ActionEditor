using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LiteAnim
{
    [System.Serializable]
    public class LayerableController : AnimController, IConnectable
    {
        struct LayerFade
        {
            public int LayerIndex;
            public float Time;
            public float Duration;
        }
        private AnimationLayerMixerPlayable layerMixerPlayable;
        private int rootIndex = -1;
        private List<LayerFade> layerFades = new List<LayerFade>();

        public void Connect<V>(V playable, int index) where V : struct, IPlayable
        {
            layerMixerPlayable.ConnectInput(index, playable, 0);
        }

        protected override void OnInit()
        {
            layerMixerPlayable = AnimationLayerMixerPlayable.Create(graph.Graph, asset.Layers.Count);
            for (int i = 0; i < asset.Layers.Count; i++)
            {
                var layer = asset.Layers[i];
                layerMixerPlayable.SetLayerAdditive((uint)i, layer.Additive);
                if (layer.Mask)
                    layerMixerPlayable.SetLayerMaskFromAvatarMask((uint)i, layer.Mask);
            }
            rootIndex = graph.ConnectToRoot(layerMixerPlayable, 0);
        }

        public override void Play(string name)
        {
            var state = GetState(name);
            if (state == null)
                return;
            int layer = state.LayerIndex;
            layerFades.RemoveAll(it=>it.LayerIndex == layer);
            for (int i = 0; i < transitions.Count; i++)
            {
                var t = transitions[i];
                if(t.LayerIndex == layer)
                {
                    if (t.To == state)
                        return;
                    if (t.From == state)
                    {
                        if (t.FadeTime > 0)
                        {
                            (t.From, t.To) = (t.To, t.From);
                            (t.FromIndex, t.ToIndex) = (t.ToIndex, t.FromIndex);
                            //如果已经在融合中，则反向融合
                            t.FadeDuration = t.FadeTime;
                            t.FadeTime = 0;
                            transitions[i] = t;
                            SetStateContinue(state);
                        }
                        else
                        {
                            //否则融合直接结束
                            OnTransitionEnd(t);
                            playingStates.RemoveAll(it => it.State == t.To);
                            transitions.RemoveAt(i);
                        }
                        return;
                    }
                }
            }
            var play = playingStates.Find(p => p.State == state);
            if(play.State != null)
            {
                SetStateContinue(state);
                return;
            }
            play = new StatePlayInfo 
            { 
                State = state,
                InputIndex = layer,
                Time = 0,
                Loop = 0
            };
            playingStates.Add(play);
            var pre = playingStates.Find(p => p.State.LayerIndex == layer);
            if(pre.State != null)
            {
                var mixer = graph.GetMixerPlayable();
                layerMixerPlayable.ConnectInput(layer, mixer, 0);
                var transition = new TransitionInfo
                {
                    LayerIndex = layer,
                    From = pre.State,
                    To = state,
                    Mixer = mixer,
                    FromIndex = 0,
                    ToIndex = 1,
                    FadeTime = 0,
                    FadeDuration = asset.DefaultFadeDuration
                };
                transitions.Add(transition);
                pre.State.Connect(mixer, 0);
                state.Connect(mixer, 1);
                mixer.SetInputWeight(0, 1);
                mixer.SetInputWeight(1, 0);
            }
            else
            {
                state.Connect(this, layer);
            }
            layerMixerPlayable.SetInputWeight(layer, 1);
        }

        public override void StopLayer(int layer)
        {
            for (int i = 0; i < layerFades.Count; i++)
            {
                if (layerFades[i].LayerIndex == layer)
                    return;
            }
            var fade = new LayerFade
            {
                LayerIndex = layer,
                Time = 0,
                Duration = asset.DefaultFadeDuration
            };
            layerFades.Add(fade);
        }

        protected override void OnUpdate(float dt)
        {
            for (int i = 0; i < layerFades.Count; i++)
            {
                var fade = layerFades[i];
                if(fade.Time > fade.Duration)
                {
                    for(int j = 0; j < transitions.Count; j++)
                    {
                        var t = transitions[j];
                        if(t.LayerIndex == fade.LayerIndex)
                        {
                            OnTransitionEnd(t);
                            playingStates.RemoveAll(it => it.State == t.From || it.State == t.To); 
                            layerMixerPlayable.DisconnectInput(fade.LayerIndex);
                            layerMixerPlayable.SetInputWeight(fade.LayerIndex, 0);
                        }
                    }
                    layerFades.RemoveAt(i);
                    --i;
                    continue;
                }
                fade.Time += dt;
                float weight = 1 - Mathf.Clamp01(fade.Time / fade.Duration);
                layerMixerPlayable.SetInputWeight(fade.LayerIndex, weight);
                layerFades[i] = fade;
            }
        }

        protected override void OnTransitionEnd(TransitionInfo transition)
        {
            layerMixerPlayable.DisconnectInput(transition.LayerIndex);
            graph.RecycleMixerPlayable(transition.Mixer);
            if(transition.To != null)
                transition.To.Connect(this, transition.LayerIndex);
            else
                layerMixerPlayable.SetInputWeight(transition.LayerIndex, 0);
        }

        protected override void OnWeightChanged()
        {
            graph.SetRootWeight(rootIndex, Weight);
        }

        protected override void UpdateTransitionWeight(TransitionInfo info, float percent)
        {
            info.Mixer.SetInputWeight(info.FromIndex, 1 - percent);
            info.Mixer.SetInputWeight(info.ToIndex, percent);
        }

        protected override bool OnStateLoop(StatePlayInfo state)
        {
            int layerIndex = state.State.LayerIndex;
            var layer = asset.Layers[layerIndex];
            if (layer.Additive)
            {
                layerMixerPlayable.DisconnectInput(layerIndex);
                layerMixerPlayable.SetInputWeight(layerIndex, 0);
                return true;
            }
            return false;
        }

    }
}
