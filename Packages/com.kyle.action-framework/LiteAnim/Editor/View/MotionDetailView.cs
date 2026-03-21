using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class MotionDetailView : VisualElement
    {
        private LiteAnimMotion target;
        public MotionDetailView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;
        }

        public void RefrshView(LiteAnimMotion motion)
        {
        }
    }
}
