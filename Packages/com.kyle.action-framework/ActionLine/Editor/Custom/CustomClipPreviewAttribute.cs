using System;

namespace ActionLine.EditorView
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class CustomClipPreviewAttribute : Attribute
    {
        public Type ClipType { get; private set; }
        public CustomClipPreviewAttribute(Type clipType)
        {
            ClipType = clipType;
        }
    }
}
