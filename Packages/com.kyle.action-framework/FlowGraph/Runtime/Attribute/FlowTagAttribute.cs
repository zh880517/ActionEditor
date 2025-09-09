using System;

namespace Flow
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class FlowTagAttribute : Attribute
    {
        public string Tag { get; private set; }
        public FlowTagAttribute(string tag)
        {
            Tag = tag;
        }
    }
}
