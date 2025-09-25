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

    public enum PlayerStatus
    {
        Stopped,
        Playing,
        Paused,
    }

    public class MontagePlayer : IMontagePlayer
    {
        #region Params
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

        public PlayerStatus Status { get; private set; } = PlayerStatus.Stopped;
        public MontageAsset Asset { get; private set; }
        private MotionStateInfo stateInfo;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixerPlayable;
        private readonly List<MontageMotionState> states = new List<MontageMotionState>();

        public void Create(string name, MontageAsset asset, Animator animator)
        {
            Asset = asset;
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
                    Status = PlayerStatus.Stopped;
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
            if (Status == PlayerStatus.Stopped)
                Status = PlayerStatus.Playing;
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
            if (Status == PlayerStatus.Stopped)
                Status = PlayerStatus.Playing;
        }


        public void Update(float dt)
        {
            if (Status != PlayerStatus.Playing)
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
