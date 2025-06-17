using UnityEditor;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ActionClipEditor
    {
        public ActionLineClip Clip { get; internal set; }
        public virtual VisualElement CreateCutomTitleElement(ActionLineClip clip) => null;
        public virtual VisualElement CreateCustomContentElement(ActionLineClip clip) => null;
        public virtual void OnClipMenu(ActionLineClip clip, int frameOffset, GenericMenu menu) { }
        public virtual void OnTitleMenu(ActionLineClip clip, GenericMenu menu) { }
        public virtual string GetClipShowName(ActionLineClip clip)
        {
            return clip.name;
        }
    }
}
