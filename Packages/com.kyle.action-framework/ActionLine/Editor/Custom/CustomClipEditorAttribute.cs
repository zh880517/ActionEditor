using System;

namespace ActionLine.EditorView
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class CustomClipEditorAttribute : Attribute
    {
        public Type ClipEditorType { get; }
        public CustomClipEditorAttribute(Type clipEditorType)
        {
            ClipEditorType = clipEditorType;
        }
    }
}
