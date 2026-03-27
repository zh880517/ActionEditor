using System;

namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class KeyColumnAttribute : Attribute
    {
        public string Name { get; private set; }
        public KeyColumnAttribute(string name)
        {
            Name = name;
        }
    }
}