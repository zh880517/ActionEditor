using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Montage
{
    public struct MotionStateInfo
    {
        public MontageMotionState State;
        public MontageMotionState Next;
        public float FadeTime;
        public float FadeDuration;
    }
    public class MontagePlayer : MontageParam
    {
        public MontageAsset Asset { get; protected set; }
        protected MotionStateInfo stateInfo;
        protected PlayableGraph graph;
        protected readonly List<MontageMotionState> states = new List<MontageMotionState>();
        protected AnimationMixerPlayable mixerPlayable;

        public void Create(string name, MontageAsset asset, Animator animator)
        {
            Asset = asset;
            graph = PlayableGraph.Create(name);
            var output = AnimationPlayableOutput.Create(graph, asset.name, animator);
            mixerPlayable = AnimationMixerPlayable.Create(graph, 0);
            output.SetSourcePlayable(mixerPlayable);
        }

        public void SetAsset(MontageAsset asset)
        {
            if (Asset == asset)
                return;
            Asset = asset;
            ResetStateInfo();
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                mixerPlayable.DisconnectInput(state.DestinationInputPort);
                state.Destroy();
            }
            states.Clear();
            mixerPlayable.SetInputCount(0);
        }

        public void EditorRebuildCheck()
        {
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (!state.Motion)
                {
                    mixerPlayable.DisconnectInput(state.DestinationInputPort);
                    state.Destroy();
                    OnStateChange(state, null);
                    for (int j = i + 1; j < states.Count; j++)
                    {
                        var s = states[j];
                        mixerPlayable.DisconnectInput(s.DestinationInputPort);
                        s.DestinationInputPort--;
                        s.Connect(mixerPlayable);
                    }
                    states.RemoveAt(i);
                    --i;
                }
            }
            if(mixerPlayable.GetInputCount() != states.Count)
            {
                mixerPlayable.SetInputCount(states.Count);
            }
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state.IsChanged)
                {
                    var newState = MontageUtil.CreateState(state.Motion);
                    newState.Player = this;
                    newState.DestinationInputPort = state.DestinationInputPort;
                    mixerPlayable.DisconnectInput(state.DestinationInputPort);
                    state.Destroy();
                    newState.Init(graph);
                    newState.Connect(mixerPlayable);
                    newState.Time = state.Time;
                    states[i] = newState;
                    OnStateChange(state, newState);
                }
            }
        }

        private void OnStateChange(MontageMotionState state, MontageMotionState newState)
        {
            if (stateInfo.State == state)
            {
                if (newState != null)
                {
                    stateInfo.State = newState;
                }
                else
                {
                    stateInfo.State = null;
                }
            }
            else if (stateInfo.Next == state)
            {
                if (newState != null)
                {
                    stateInfo.Next = newState;
                }
                else
                {
                    stateInfo.Next = null;
                    stateInfo.FadeTime = 0;
                    stateInfo.FadeDuration = 0;
                }
            }
        }

        private MontageMotionState GetState(string name)
        {
            var state = states.Find(it => it.Name == name);
            if(state == null)
            {
                var motion = Asset.Motions.Find(it => it.name == name);
                if(!motion)
                    return null;
                mixerPlayable.SetInputCount(states.Count + 1);
                state = MontageUtil.CreateState(motion);
                state.Player = this;
                state.DestinationInputPort = states.Count;
                state.Init(graph);
                state.Connect(mixerPlayable);
                states.Add(state);
            }
            return state;
        }

        private void ResetStateInfo()
        {
            if(stateInfo.State != null)
            {
                stateInfo.State.Time = 0;
            }
            if (stateInfo.Next != null)
            {
                stateInfo.Next.Time = 0;
            }
            stateInfo = new MotionStateInfo();
        }

        public void Play(string name)
        {
            var state = GetState(name);
            if (state == null)
                return;
            ResetStateInfo();
            stateInfo.State = state;
        }

        public void CrossFade(string name)
        {
            var state = GetState(name);
            if (state == null)
                return;
            if(stateInfo.State == state)
            {
                if(stateInfo.Next != null)
                {
                    stateInfo.Next.Time = 0;
                    stateInfo.Next = null;
                    stateInfo.FadeTime = 0;
                    stateInfo.FadeDuration = 0;
                }
                return;
            }
            if (stateInfo.Next == state)
                return;
            if (stateInfo.State == null)
            {
                stateInfo.State = state;
                return;
            }
            else
            {
                stateInfo.Next = state;
                stateInfo.FadeTime = 0;
                stateInfo.FadeDuration = Asset.DefaultFadeDuration;
            }
        }


        public void Update(float dt)
        {
            if (stateInfo.State == null)
                return;
            float weight = 1;
            //处理动画的淡入淡出时间
            if (stateInfo.Next != null)
            {
                stateInfo.FadeTime += dt;
                if (stateInfo.FadeTime < stateInfo.FadeDuration)
                {
                    weight = 1 - Mathf.Clamp01(stateInfo.FadeTime / stateInfo.FadeDuration);
                }
                else
                {
                    stateInfo.State.Time = 0;
                    stateInfo.State = stateInfo.Next;
                    stateInfo.Next = null;
                    stateInfo.FadeTime = 0;
                    stateInfo.FadeDuration = 0;
                }
            }
            //刷新所有状态的时间和权重
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state == stateInfo.State)
                {
                    state.Weight = weight;
                    mixerPlayable.SetInputWeight(state.DestinationInputPort, weight);
                    state.Time += dt;
                    state.OnUpdate();
                }
                else if (state == stateInfo.Next)
                {
                    state.Weight = 1 - weight;
                    mixerPlayable.SetInputWeight(state.DestinationInputPort, 1 - weight);
                    state.Time += dt;
                    state.OnUpdate();
                }
                else
                {
                    state.Time = 0;
                    state.Weight = 0;
                    mixerPlayable.SetInputWeight(state.DestinationInputPort, 0);
                }
            }
            //动作时间由State直接控制，不在这里累加
            graph.Evaluate(0);
        }
    }
}
