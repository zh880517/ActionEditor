using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace LiteAnim.EditorView
{
    public class LiteAnimPreview : ScriptableObject
    {
        public GameObject ModelInScen;
        public Animator Animator;
        [SerializeField]
        private GameObject currentPrefab;
        private PlayableGraph playableGraph;

        public void OnPreviewChange(GameObject prefab, bool enable)
        {
            if((currentPrefab != prefab || !enable) && ModelInScen)
            {
                DestroyImmediate(ModelInScen);
                ModelInScen = null;
                Animator = null;
                return;
            }
            if (!prefab || !enable)
                return;

            ModelInScen = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ModelInScen.hideFlags = HideFlags.DontSave;
            Animator = ModelInScen.GetComponentInChildren<Animator>();
        }

        public void Evaluate(LiteAnimMotion motion, float time)
        {

        }
    }
}
