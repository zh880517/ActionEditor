using System;
namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldSeparatorAttribute : Attribute
    {
        public char Separator { get; private set; }
        public FieldSeparatorAttribute(char sep)
        {
            Separator = sep;
        }
    }
}