using UnityEngine;
using UnityEngine.Playables;

namespace LiteAnim.EditorView
{
    public class LiteAnimPreview : ScriptableObject
    {
        public GameObject ModelInScen;
        public Animator Animator;
        public PlayableGraph Graph {  get; private set; }
    }
}
