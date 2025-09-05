using System;

namespace PropertyEditor
{
    [AttributeUsageAttribute(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class CustomPropertyAttribute : Attribute
    {
    }
}
