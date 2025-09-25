using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Animatic
{
    public struct AnimaticStateInfo
    {
        public AnimaticMotionState State;
        public AnimaticMotionState Next;
        public float FadeTime;
        public float FadeDuration;
    }

    public enum AnimaticPlayerStatus
    {
        Stopped,
        Playing,
        Paused,
    }

    public class AnimaticController
    {
        #region Params 待移出
        public struct ParamInfo
        {
            public string Name;
            public float Value;
        }
        protected List<ParamInfo> paramInfos = new List<ParamInfo>();

        public void SetParam(string name, float value)
        {
            for (int i = 0; i < paramInfos.Count; i++)
            {
                var p = paramInfos[i];
                if (p.Name == name)
                {
                    p.Value = value;
                    paramInfos[i] = p;
                    return;
                }
            }
            paramInfos.Add(new ParamInfo { Name = name, Value = value });
        }

        public float GetParam(string name)
        {
            for (int i = 0; i < paramInfos.Count; i++)
            {
                var p = paramInfos[i];
                if (p.Name == name)
                {
                    return p.Value;
                }
            }
            return 0;
        }
        #endregion

        public AnimaticPlayerStatus Status { get; private set; } = AnimaticPlayerStatus.Stopped;
        public AnimaticAsset Asset { get; private set; }
        private IAnimaticPlayer player;
        private AnimaticStateInfo stateInfo;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixerPlayable;
        private readonly List<AnimaticMotionState> states = new List<AnimaticMotionState>();

        public void Create(string name, AnimaticAsset asset, Animator animator, IAnimaticPlayer player)
        {
            Asset = asset;
            this.player = player;
            graph = PlayableGraph.Create(name);
            var output = AnimationPlayableOutput.Create(graph, asset.name, animator);
            mixerPlayable = AnimationMixerPlayable.Create(graph, 0);
            output.SetSourcePlayable(mixerPlayable);
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
                        s.DestinationInputPort = s.DestinationInputPort - 1;
                        s.Connect(mixerPlayable);
                    }
                    states.RemoveAt(i);
                    --i;
                }
            }
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state.IsChanged)
                {
                    var newState = AnimaticUtil.CreateState(state.Motion);
                    newState.Player = player;
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

        private void OnStateChange(AnimaticMotionState state, AnimaticMotionState newState)
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
                    Status = AnimaticPlayerStatus.Stopped;
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

        private AnimaticMotionState GetState(string name)
        {
            var state = states.Find(it => it.Name == name);
            if(state == null)
            {
                var motion = Asset.Motions.Find(it => it.name == name);
                if(!motion)
                    return null;
                mixerPlayable.SetInputCount(states.Count + 1);
                state = AnimaticUtil.CreateState(motion);
                state.Player = player;
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
            stateInfo = new AnimaticStateInfo();
        }

        public void Play(string name)
        {
            var state = GetState(name);
            if (state == null)
                return;
            ResetStateInfo();
            stateInfo.State = state;
            if (Status == AnimaticPlayerStatus.Stopped)
                Status = AnimaticPlayerStatus.Playing;
        }

        public void CrossFade(string name)
        {
            var state = GetState(name);
            if (state == null)
                return;

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
            if (Status == AnimaticPlayerStatus.Stopped)
                Status = AnimaticPlayerStatus.Playing;
        }


        public void Update(float dt)
        {
            if (Status != AnimaticPlayerStatus.Playing)
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
                    mixerPlayable.SetInputWeight(state.DestinationInputPort, weight);
                    state.Time += dt;
                    state.OnUpdate();
                }
                else if (state == stateInfo.Next)
                {
                    mixerPlayable.SetInputWeight(state.DestinationInputPort, 1 - weight);
                    state.Time += dt;
                    state.OnUpdate();
                }
                else
                {
                    state.Time = 0;
                    mixerPlayable.SetInputWeight(state.DestinationInputPort, 0);
                }
            }
            //动作时间由State直接控制，不在这里累加
            graph.Evaluate(0);
        }
    }
}
