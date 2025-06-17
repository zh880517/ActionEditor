using System;

namespace ActionLine.EditorView
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class CustomClipEditorAttribute : Attribute
    {
        public Type ClipEditorType { get; }
        public CustomClipEditorAttribute(Type clipEditorType)
        {
            if (clipEditorType == null)
            {
                throw new ArgumentNullException(nameof(clipEditorType), "Clip editor type cannot be null.");
            }
            if (!typeof(ActionClipEditor).IsAssignableFrom(clipEditorType))
            {
                throw new ArgumentException("The specified type must implement IClipEditor.", nameof(clipEditorType));
            }
            ClipEditorType = clipEditorType;
        }
    }
}
