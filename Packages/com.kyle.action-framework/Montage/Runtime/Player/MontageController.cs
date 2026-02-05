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
        public int TargetInputIndex;
        public int FromIndex;
        public int ToIndex;
        public float FadeDuration;
        public float FadeTime;
        public AnimationMixerPlayable Mixer;
    }

    public struct MontageStatePlayInfo
    {
        public MontageMotionState State;
        public int InputIndex;
        public float Time;
        public int Loop;
    }

    [System.Serializable]
    public abstract class MontageController
    {
        [SerializeField]
        protected MontageAsset asset;
        protected MontageGraph graph;
        protected IMontagePlayer player;
        protected readonly List<MontageMotionState> states = new List<MontageMotionState>();
        protected readonly List<MontageStatePlayInfo> playing = new List<MontageStatePlayInfo>();
        protected readonly List<MontageTransitionInfo> transitions = new List<MontageTransitionInfo>();
        public float Weight { get; private set; }

        public IReadOnlyList<MontageStatePlayInfo> Playing => playing;
        public IReadOnlyList<MontageTransitionInfo> Transitions => transitions;

        public void Init(MontageAsset asset, MontageGraph graph, IMontagePlayer player)
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

        protected abstract void OnWeightChanged();

        protected abstract void OnInit();

        public void Update(float dt)
        {
            if (playing.Count == 0)
                return;
            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                if(transition.FadeDuration >= transition.FadeTime)
                {
                    transitions.RemoveAt(i);
                    i--;
                    OnTransitionEnd(transition);
                    continue;
                }
                transition.FadeTime += dt;
                float weight = Mathf.Clamp01(transition.FadeTime / transition.FadeDuration);
                transition.Mixer.SetInputWeight(transition.FromIndex, 1 - weight);
                transition.Mixer.SetInputWeight(transition.ToIndex, weight);
                transitions[i] = transition;
            }
            for (int i = 0; i < playing.Count; i++)
            {
                var state = playing[i];
                state.Time += dt;
                double time = GetStateTime(state.State, state.Time, out state.Loop);
                state.State.Evaluate(time);
                playing[i] = state;
            }
        }

        private void OnTransitionEnd(MontageTransitionInfo transition)
        {
        }

        private void OnStatePlayEnd(MontageMotionState state)
        {
        }

        protected MontageMotionState GetState(string name)
        {
            var state = states.Find(it => it.Name == name);
            if (state == null)
            {
                var motion = asset.Motions.Find(it => it.name == name);
                if (!motion)
                    return null;
                //mixerPlayable.SetInputCount(states.Count + 1);
                state = MontageUtil.CreateState(motion);
                state.Player = player;
                state.Init(graph.Graph);
                //state.Connect(mixerPlayable);
                states.Add(state);
            }
            return state;
        }


        public float GetStateTime(MontageMotionState state, float time, out int loop)
        {
            loop = 0;
            if (time < 0)
                return 0;
            if (time > state.Length)
            {
                switch (state.Motion.WrapMode)
                {
                    case MotionWrapMode.Clamp:
                        loop = 1;
                        time = state.Length;
                        break;
                    case MotionWrapMode.Loop:
                        loop = (int)(time/state.Length);
                        time %= state.Length;
                        break;
                    case MotionWrapMode.PingPong:
                        loop = (int)(time / state.Length);
                        float length2 = state.Length * 2;
                        time = time % length2;
                        if (time > state.Length)
                            time = length2 - time;

                        break;
                }
            }
            return time;
        }
    }
}
