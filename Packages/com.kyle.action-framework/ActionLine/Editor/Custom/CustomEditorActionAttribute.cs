using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class CustomEditorActionAttribute : Attribute
{
    public Type AssetType { get; private set; }

    public CustomEditorActionAttribute(Type type)
    {
        AssetType = type;
    }
}
