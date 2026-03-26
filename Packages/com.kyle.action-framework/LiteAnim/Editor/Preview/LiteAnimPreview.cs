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
            if (playableGraph.IsValid())
                playableGraph.Destroy();
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
