using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LiteAnim.EditorView
{
    public class LiteAnimPreview : ScriptableObject, ILiteAnimPlayer
    {
        public GameObject ModelInScen;
        public Animator Animator;
        [SerializeField]
        private GameObject currentPrefab;
        private PlayableGraph playableGraph;
        private MotionState currentState;
        private LiteAnimMotion currentMotion;
        private AnimationMixerPlayable rootMixer;
        private Dictionary<string, float> animParams = new Dictionary<string, float>();

        // ---- 融合预览 ----
        private MotionState transFromState;
        private MotionState transToState;
        private LiteAnimMotion transFromMotion;
        private LiteAnimMotion transToMotion;
        private float transFadeDuration;

        public void OnPreviewChange(GameObject prefab, bool enable)
        {
            if((currentPrefab != prefab || !enable) && ModelInScen)
            {
                DestroyPreviewState();
                DestroyImmediate(ModelInScen);
                ModelInScen = null;
                Animator = null;
                currentPrefab = null;
                return;
            }
            if (!prefab || !enable)
                return;

            currentPrefab = prefab;
            ModelInScen = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ModelInScen.hideFlags = HideFlags.DontSave;
            Animator = ModelInScen.GetComponentInChildren<Animator>();
        }

        public void Evaluate(LiteAnimMotion motion, float time)
        {
            if (!ModelInScen || !Animator)
                return;
            if (!motion || !motion.IsValid())
            {
                DestroyPreviewState();
                return;
            }

            // 重建：Motion 变更或资源被修改
            if (currentMotion != motion || currentState == null || currentState.IsChanged)
            {
                RebuildPlayableGraph(motion);
            }

            if (currentState == null)
                return;

            currentState.Evaluate(time);
            playableGraph.Evaluate();
            SceneView.RepaintAll();
        }

        public void SetParam(string name, float value)
        {
            animParams[name] = value;
        }

        private void RebuildPlayableGraph(LiteAnimMotion motion)
        {
            DestroyPreviewState();
            currentMotion = motion;
            currentState = LiteAnimUtil.CreateState(motion);
            if (currentState == null)
                return;

            currentState.Player = this;
            currentState.Motion = motion;
            playableGraph = PlayableGraph.Create("LiteAnimPreview");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            var output = AnimationPlayableOutput.Create(playableGraph, "Preview", Animator);
            rootMixer = AnimationMixerPlayable.Create(playableGraph, 1);
            output.SetSourcePlayable(rootMixer);

            currentState.Create(playableGraph);
            currentState.Connect(rootMixer, 0);
            rootMixer.SetInputWeight(0, 1f);
        }

        private void DestroyPreviewState()
        {
            currentState?.Destroy();
            currentState = null;
            currentMotion = null;
            transFromState?.Destroy();
            transFromState = null;
            transToState?.Destroy();
            transToState = null;
            transFromMotion = null;
            transToMotion = null;
            if (playableGraph.IsValid())
                playableGraph.Destroy();
        }

        /// <summary>
        /// 预览两个 Motion 之间的融合过渡
        /// time 从 0 开始，当 time 超过 fromMotion 长度 - fadeDuration 时开始融合
        /// </summary>
        public void EvaluateTransition(LiteAnimMotion from, LiteAnimMotion to, float fadeDuration, float time)
        {
            if (!ModelInScen || !Animator)
                return;
            if (!from || !from.IsValid() || !to || !to.IsValid())
            {
                DestroyPreviewState();
                return;
            }

            // 重建: Motion 变更或资源被修改
            if (transFromMotion != from || transToMotion != to
                || transFromState == null || transToState == null
                || transFromState.IsChanged || transToState.IsChanged
                || !Mathf.Approximately(transFadeDuration, fadeDuration))
            {
                RebuildTransitionGraph(from, to, fadeDuration);
            }

            if (transFromState == null || transToState == null)
                return;

            float fromLen = from.GetLength();
            float fadeStart = Mathf.Max(0, fromLen - fadeDuration);

            // From 动画时间
            float fromTime = Mathf.Clamp(time, 0, fromLen);
            transFromState.Evaluate(fromTime);

            // To 动画时间（从融合开始计算）
            float toTime = Mathf.Max(0, time - fadeStart);
            transToState.Evaluate(toTime);

            // 计算融合权重
            float weight;
            if (time <= fadeStart)
                weight = 0;
            else if (fadeDuration > 0)
                weight = Mathf.Clamp01((time - fadeStart) / fadeDuration);
            else
                weight = time >= fadeStart ? 1f : 0f;

            rootMixer.SetInputWeight(0, 1f - weight);
            rootMixer.SetInputWeight(1, weight);

            playableGraph.Evaluate();
            SceneView.RepaintAll();
        }

        private void RebuildTransitionGraph(LiteAnimMotion from, LiteAnimMotion to, float fadeDuration)
        {
            DestroyPreviewState();
            transFromMotion = from;
            transToMotion = to;
            transFadeDuration = fadeDuration;

            transFromState = LiteAnimUtil.CreateState(from);
            transToState = LiteAnimUtil.CreateState(to);
            if (transFromState == null || transToState == null)
            {
                transFromState?.Destroy();
                transToState?.Destroy();
                transFromState = null;
                transToState = null;
                return;
            }

            transFromState.Player = this;
            transFromState.Motion = from;
            transToState.Player = this;
            transToState.Motion = to;

            playableGraph = PlayableGraph.Create("LiteAnimTransitionPreview");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            var output = AnimationPlayableOutput.Create(playableGraph, "TransitionPreview", Animator);
            rootMixer = AnimationMixerPlayable.Create(playableGraph, 2);
            output.SetSourcePlayable(rootMixer);

            transFromState.Create(playableGraph);
            transToState.Create(playableGraph);
            transFromState.Connect(rootMixer, 0);
            transToState.Connect(rootMixer, 1);
            rootMixer.SetInputWeight(0, 1f);
            rootMixer.SetInputWeight(1, 0f);
        }

        private void OnDisable()
        {
            DestroyPreviewState();
        }

        public float GetParam(string name)
        {
            if(string.IsNullOrEmpty(name) || !animParams.TryGetValue(name, out var v))
                return 0f;
            return v;
        }
    }
}
