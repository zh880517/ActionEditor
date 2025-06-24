using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ActionClipEditor
    {
        public ActionLineClip Clip { get; internal set; }
        public virtual VisualElement CreateCutomTitleElement(ActionLineClip clip) => null;
        public virtual VisualElement CreateCustomContentElement(ActionLineClip clip) => null;
        public virtual void OnClipMenu(ActionLineClip clip, int frameOffset, GenericMenu menu) { }
        //只有按下Alt键并且当前帧在 Clip 的范围内才会调用
        public virtual bool OnKeyDown(ActionLineClip clip, bool shiftDown, KeyCode keyCode, int frameOffset) { return false; }
        public virtual void OnTitleMenu(ActionLineClip clip, GenericMenu menu) { }
        public virtual string GetClipShowName(ActionLineClip clip)
        {
            return clip.name;
        }
    }
}
