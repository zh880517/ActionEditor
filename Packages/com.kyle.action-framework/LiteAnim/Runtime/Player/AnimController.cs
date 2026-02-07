using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LiteAnim
{
    public struct TransitionInfo
    {
        public MotionState From;
        public MotionState To;
        public int LayerIndex;
        public int FromIndex;
        public int ToIndex;
        public float FadeDuration;
        public float FadeTime;
        public AnimationMixerPlayable Mixer;
    }

    public struct StatePlayInfo
    {
        public MotionState State;
        public int InputIndex;
        public float Time;
        public int Loop;
        public int MaxLoop;
        public bool Looped;
    }

    [System.Serializable]
    public abstract class AnimController
    {
        [SerializeField]
        protected LiteAnimAsset asset;
        protected LiteAnimGraph graph;
        protected ILiteAnimPlayer player;
        protected readonly List<MotionState> states = new List<MotionState>();
        protected readonly List<StatePlayInfo> playingStates = new List<StatePlayInfo>();
        protected readonly List<TransitionInfo> transitions = new List<TransitionInfo>();
        public float Weight { get; private set; }

        public IReadOnlyList<StatePlayInfo> Playing => playingStates;
        public IReadOnlyList<TransitionInfo> Transitions => transitions;

        public void Init(LiteAnimAsset asset, LiteAnimGraph graph, ILiteAnimPlayer player)
        {
            this.asset = asset;
            this.graph = graph;
            this.player = player;
            OnInit();
        }

        public void SetWeight(float weight)
        {
            Weight = weight;
            OnWeightChanged();
        }

        public abstract void Play(string name);
        public virtual void StopLayer(int layer) { }
        protected abstract void OnWeightChanged();

        protected abstract void OnInit();

        public void Update(float dt)
        {
            if (playingStates.Count == 0)
                return;
            //对于计时删除的内容，满足删除条件后在下一帧删除，不然会看不到对应动画的最后一帧
            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                if(transition.FadeDuration >= transition.FadeTime)
                {
                    transitions.RemoveAt(i);
                    i--;
                    OnTransitionEnd(transition);
                    playingStates.RemoveAll(it => it.State == transition.From);
                    continue;
                }
                transition.FadeTime += dt;
                transitions[i] = transition;
                float weight = Mathf.Clamp01(transition.FadeTime / transition.FadeDuration);
                UpdateTransitionWeight(transition, weight);
            }
            for (int i = 0; i < playingStates.Count; i++)
            {
                var play = playingStates[i];
                if (play.Looped && OnStateLoop(play))
                {
                    playingStates.RemoveAt(i);
                    --i;
                    continue;
                }
                play.Time += dt;
                int loop = play.Loop;
                if (play.Time > play.State.Length)
                {
                    play.Loop++;
                    play.Looped = true;
                    if (play.MaxLoop > 0 && play.Loop >= play.MaxLoop)
                        play.Time = play.State.Length;
                    else
                        play.Time %= play.State.Length;
                }
                play.State.Evaluate(play.Time);
                playingStates[i] = play;
            }
            OnUpdate(dt);
        }
        protected virtual void OnUpdate(float dt) { }
        protected abstract void UpdateTransitionWeight(TransitionInfo info, float percent);

        protected abstract void OnTransitionEnd(TransitionInfo transition);

        protected MotionState GetState(string name)
        {
            var state = states.Find(it => it.Name == name);
            if (state == null)
            {
                var motion = asset.Motions.Find(it => it.name == name);
                if (!motion)
                    return null;
                state = LiteAnimUtil.CreateState(motion);
                state.Player = player;
                state.Create(graph.Graph);
                states.Add(state);
            }
            return state;
        }
        protected void SetStateContinue(MotionState state)
        {
            if (!state.Motion.Loop)
            {
                int playIndex = playingStates.FindIndex(it => it.State == state);
                if (playIndex >= 0)
                {
                    var exist = playingStates[playIndex];
                    if((exist.MaxLoop - exist.Loop) <= 1 && exist.Time / state.Length > 0.5f)
                    {
                        exist.MaxLoop++;
                        playingStates[playIndex] = exist;
                    }
                }
            }
        }
        protected virtual bool OnStateLoop(StatePlayInfo state)
        {
            return false;
        }

        public virtual void Destroy()
        {
            foreach (var state in states)
            {
                state.Destroy();
            }
            foreach (var item in transitions)
            {
                if(item.Mixer.IsValid())
                    graph.RecycleMixerPlayable(item.Mixer);
            }
            states.Clear();
            playingStates.Clear();
            transitions.Clear();
        }

        public static float GetStateTime(MotionState state, float time, out int loop)
        {
            loop = 0;
            if (time < 0)
                return 0;
            if (time >= state.Length)
            {
                if(state.Motion.Loop)
                {
                    loop = (int)(time / state.Length);
                    return time % state.Length;
                }
                else
                {
                    loop = 1;
                    return state.Length;
                }
            }
            return time;
        }

    }
}
