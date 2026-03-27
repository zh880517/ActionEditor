using System;
namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DictionarySeparatorAttribute : Attribute
    {
        public char Element;
        public char KeyValue;

        public DictionarySeparatorAttribute(char element, char keyValue)
        {
            Element = element;
            KeyValue = keyValue;
        }
    }
}
