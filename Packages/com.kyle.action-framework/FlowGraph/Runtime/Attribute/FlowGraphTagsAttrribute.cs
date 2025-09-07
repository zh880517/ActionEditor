using System;

namespace Flow
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class FlowGraphTagsAttrribute : Attribute
    {
        public string[] Tags { get; private set; }
        public FlowGraphTagsAttrribute(params string[] tags)
        {
            Tags = tags;
        }
    }
}
