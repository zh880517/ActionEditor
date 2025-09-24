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

    public class AnimaticPlayer
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

        public AnimaticAsset Asset { get; private set; }
        private AnimaticStateInfo stateInfo;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixerPlayable;
        private readonly List<AnimaticMotionState> states = new List<AnimaticMotionState>();

        public void Create(string name, AnimaticAsset asset, Animator animator, IAnimaticPlayer player)
        {
            Asset = asset;
            foreach (var item in asset.Motions)
            {
                if(item.Valid)
                {
                    var state = AnimaticUtil.CreateState(item);
                    state.Player = player;
                    states.Add(state);
                }
            }
            graph = PlayableGraph.Create(name);
            var output = AnimationPlayableOutput.Create(graph, asset.name, animator);
            mixerPlayable = AnimationMixerPlayable.Create(graph, states.Count);
            output.SetSourcePlayable(mixerPlayable);
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                state.DestinationInputPort = i;
                state.Init(graph);
                state.Connect(mixerPlayable);
            }
        }

        public void Play(string name)
        {

        }

        public void CrossFade(string name)
        {

        }


        public void Update(float dt)
        {
            graph.Evaluate(dt);
        }
    }
}
