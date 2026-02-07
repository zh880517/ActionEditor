using UnityEngine;

namespace LiteAnim
{
    [System.Serializable]
    public class MixableController : AnimController
    {
        public override void Play(string name)
        {
            var state = GetState(name);
            if (state == null)
                return;
            //该模式只能有一个融合
            if(transitions.Count > 0)
            {
                var t = transitions[0];
                if (t.To.Name == name)
                    return;
                if (t.From.Name == name)
                {
                    (t.From, t.To) = (t.To, t.From);
                    (t.FromIndex, t.ToIndex) = (t.ToIndex, t.FromIndex);
                    if (t.FadeTime > 0)
                    {   
                        //如果已经在融合中，则反向融合
                        t.FadeDuration = t.FadeTime;
                        t.FadeTime = 0;
                        transitions[0] = t;
                        if(!state.Motion.Loop)
                        {
                            int playIndex = playingStates.FindIndex(it => it.State == state);
                            if (playIndex >= 0)
                            {
                                var exist = playingStates[playIndex];
                                exist.MaxLoop++;
                                playingStates[playIndex] = exist;
                            }
                        }
                    }
                    else
                    {
                        //否则融合直接结束
                        OnTransitionEnd(t);
                        playingStates.RemoveAll(it => it.State == t.To);
                        transitions.Clear();
                    }
                    return;
                }
                else
                {
                    //如果当前动画不在融合中，则直接结束当前融合
                    OnTransitionEnd(t);
                    playingStates.RemoveAll(it => it.State == t.From);
                    transitions.Clear();
                }
            }
            else
            {
                if (playingStates.Exists(p => p.State == state))
                    return;
            }
            int index = graph.GetEmptyRootIndex();
            state.Connect(graph, index);
            if(playingStates.Count > 0)
            {
                var last = playingStates[^1];
                var tran = new TransitionInfo
                {
                    From = last.State,
                    To = state,
                    FromIndex = last.InputIndex,
                    ToIndex = index,
                    FadeDuration = asset.DefaultFadeDuration,
                };
                transitions.Add(tran);
            }
            playingStates.Add(new StatePlayInfo
            {
                State = state,
                InputIndex = index,
                Time = 0,
                Loop = 0,
                MaxLoop = state.Motion.Loop ? -1 : 1,
            });
        }

        protected override void OnInit()
        {
        }

        protected override void OnTransitionEnd(TransitionInfo transition)
        {
            graph.DisConnect(transition.FromIndex);
            graph.SetRootWeight(transition.ToIndex, Weight);
        }

        protected override void OnWeightChanged()
        {
            if (transitions.Count > 0) 
            {
                for (int i = 0; i < transitions.Count; i++)
                {
                    var t = transitions[i];
                    float weight = Mathf.Clamp01(t.FadeTime / t.FadeDuration);
                    graph.SetRootWeight(t.ToIndex, (weight) * Weight);
                    graph.SetRootWeight(t.FromIndex, (1 - weight) * Weight);
                }
            }
            else
            {
                for (int i = 0; i < playingStates.Count; i++)
                {
                    var p = playingStates[i];
                    graph.SetRootWeight(p.InputIndex, Weight);
                }
            }
            
        }

        protected override void UpdateTransitionWeight(TransitionInfo t, float percent)
        {
            graph.SetRootWeight(t.ToIndex, (percent) * Weight);
            graph.SetRootWeight(t.FromIndex, (1 - percent) * Weight);
        }
    }
}
