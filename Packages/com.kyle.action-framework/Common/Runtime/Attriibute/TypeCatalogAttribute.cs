using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TypeCatalogAttribute : Attribute
{
    public string Name { get; private set; }
    public TypeCatalogAttribute(string name)
    {
        Name = name;
    }
}