using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AliasAttribute : Attribute
{
    public string Name { get; private set; }
    public AliasAttribute(string alias)
    {
        Name = alias;
    }
}