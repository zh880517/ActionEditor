using System;
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class CustomPropertyElementAttribute : Attribute
{
    public Type DataType { get; private set; }
    public int Priority { get; private set; }
    public CustomPropertyElementAttribute(Type dataType, int priority = 0)
    {
        DataType = dataType;
        Priority = priority;
    }
}
