using UnityEngine;
using UnityEngine.Playables;

namespace LiteAnim.EditorView
{
    [System.Serializable]
    public class LiteAnimPreview
    {
        public GameObject ModelInScen;
        public Animator Animator;
        public PlayableGraph Graph {  get; private set; }
    }
}
