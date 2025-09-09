using System;

namespace Flow
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class FlowNodePathAttribute : Attribute
    {
        public string Path { get; private set; }
        public FlowNodePathAttribute(string path)
        {
            Path = path;
        }
    }
}
