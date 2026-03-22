using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public abstract class MotionEditorView : VisualElement
    {
        public abstract MotionType Type { get; }

        public abstract void Refresh(LiteAnimMotion motion);
    }
}
