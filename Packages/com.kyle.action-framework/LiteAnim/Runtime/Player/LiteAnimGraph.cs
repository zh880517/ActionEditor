using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LiteAnim
{
    [System.Serializable]
    public class LiteAnimGraph : IConnectable
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
        private Stack<AnimationMixerPlayable> mixerPlayables;//回收池

        public static LiteAnimGraph Create(string name, Animator target)
        {
            var montageGraph = new LiteAnimGraph();
            montageGraph.Graph = PlayableGraph.Create(name);
            montageGraph.Target = target;
            montageGraph.output = AnimationPlayableOutput.Create(montageGraph.Graph, "MontageOutput", target);
            montageGraph.rootMixer = AnimationMixerPlayable.Create(montageGraph.Graph, 2);
            montageGraph.rootMixer.SetInputWeight(0, 0f);
            montageGraph.rootMixer.SetInputWeight(1, 0f);
            montageGraph.output.SetSourcePlayable(montageGraph.rootMixer);
            return montageGraph;
        }

        public int ConnectToRoot<V>(V playable, float weight) where V : struct, IPlayable
        {
            for (int i = 0; i < connectInfos.Length; i++)
            {
                if(connectInfos[i] == playable.GetHandle())
                {
                    //已经连接过了，更新权重
                    rootMixer.SetInputWeight(i, weight);
                    var info = connectInfos[i];
                    connectInfos[i] = info;
                    return i;
                }
                else if(connectInfos[i] == PlayableHandle.Null)
                {
                    //找到空位，复用
                    playable.ConnectInput(i, rootMixer, 0);
                    rootMixer.SetInputWeight(i, weight);
                    connectInfos[i] = playable.GetHandle();
                    return i;
                }
            }
            //没有空位，扩容
            int nexIndex = connectInfos.Length;
            Array.Resize(ref connectInfos, connectInfos.Length + 1);
            rootMixer.SetInputCount(connectInfos.Length);
            playable.ConnectInput(nexIndex, rootMixer, 0);
            rootMixer.SetInputWeight(nexIndex, weight);
            connectInfos[nexIndex] = playable.GetHandle();
            return nexIndex;
        }

        public void SetRootWeight(int index, float weight)
        {
            if (index >= 0 && index < connectInfos.Length)
            {
                var info = connectInfos[index];
                if (info == PlayableHandle.Null)
                    return;
                rootMixer.SetInputWeight(index, weight);
            }
        }

        public void Connect<V>(V playable, int index) where V : struct, IPlayable
        {
            if (index >= 0 && index < connectInfos.Length)
            {
                playable.ConnectInput(index, rootMixer, 0);
                connectInfos[index] = playable.GetHandle();
            }
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
            mixerPlayables ??= new Stack<AnimationMixerPlayable>();
            if (mixerPlayables.Count == 0)
                return AnimationMixerPlayable.Create(Graph, 2);
            return mixerPlayables.Pop();
        }
        public void RecycleMixerPlayable(AnimationMixerPlayable mixer)
        {
            if (mixerPlayables == null)
                return;
            for (int i = 0; i < mixer.GetInputCount(); i++)
            {
                mixer.DisconnectInput(i);
            }
            mixerPlayables.Push(mixer);
        }

        public void Destroy()
        {
            if(mixerPlayables != null)
            {
                foreach (var item in mixerPlayables)
                {
                    item.Destroy();
                }
                mixerPlayables.Clear();
            }
            rootMixer.Destroy();
            Graph.Destroy();
            Target = null;
        }
    }
}
