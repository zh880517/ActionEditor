using System;

namespace ActionLine.EditorView
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CustomEditorProviderAttribute : Attribute
    {
        public Type AssetType { get; private set; }
        public CustomEditorProviderAttribute(Type assetType)
        {
            AssetType = assetType;
        }
    }
}
