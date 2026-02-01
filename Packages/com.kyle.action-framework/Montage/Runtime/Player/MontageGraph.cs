using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Montage
{
    [System.Serializable]
    public class MontageGraph
    {
        private struct ConnectInfo
        {
            public PlayableHandle Handler;
            public float Weight;
        }

        public PlayableGraph Graph;
        public Animator Target;
        private AnimationPlayableOutput output;
        private AnimationMixerPlayable rootMixer;
        private PlayableHandle[] connectInfos = new PlayableHandle[2];
        private readonly Stack<AnimationMixerPlayable> mixerPlayables = new Stack<AnimationMixerPlayable>();//回收池

        public static MontageGraph Create(string name, Animator target)
        {
            var montageGraph = new MontageGraph();
            montageGraph.Graph = PlayableGraph.Create(name);
            montageGraph.Target = target;
            montageGraph.output = AnimationPlayableOutput.Create(montageGraph.Graph, "MontageOutput", target);
            montageGraph.rootMixer = AnimationMixerPlayable.Create(montageGraph.Graph, 2);
            montageGraph.rootMixer.SetInputWeight(0, 0f);
            montageGraph.rootMixer.SetInputWeight(1, 0f);
            montageGraph.output.SetSourcePlayable(montageGraph.rootMixer);
            return montageGraph;
        }

        public void ConnectToRoot<V>(V destination, float weight) where V : struct, IPlayable
        {
            for (int i = 0; i < connectInfos.Length; i++)
            {
                if(connectInfos[i] == destination.GetHandle())
                {
                    //已经连接过了，更新权重
                    rootMixer.SetInputWeight(i, weight);
                    var info = connectInfos[i];
                    connectInfos[i] = info;
                    return;
                }
                else if(connectInfos[i] == PlayableHandle.Null)
                {
                    //找到空位，复用
                    destination.ConnectInput(i, rootMixer, 0);
                    rootMixer.SetInputWeight(i, weight);
                    connectInfos[i] = destination.GetHandle();
                    return;
                }
            }
            //没有空位，扩容
            int nexIndex = connectInfos.Length;
            Array.Resize(ref connectInfos, connectInfos.Length + 1);
            rootMixer.SetInputCount(connectInfos.Length);
            destination.ConnectInput(nexIndex, rootMixer, 0);
            rootMixer.SetInputWeight(nexIndex, weight);
            connectInfos[nexIndex] = destination.GetHandle();
        }

        public void DisConnectFromRoot<V>(V playable) where V : struct, IPlayable
        {
            for (int i = 0; i < connectInfos.Length; i++)
            {
                if(connectInfos[i] == playable.GetHandle())
                {
                    rootMixer.DisconnectInput(i);
                    rootMixer.SetInputWeight(i, 0f);
                    connectInfos[i] = PlayableHandle.Null;
                    return;
                }
            }
        }

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
