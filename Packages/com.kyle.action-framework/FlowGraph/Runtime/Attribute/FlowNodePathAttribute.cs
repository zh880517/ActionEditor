using System;

namespace Flow
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class FlowNodePathAttribute : Attribute
    {
        public string Path { get; private set; }
        public FlowNodePathAttribute(string path)
        {
            Path = path;
        }
    }
}
