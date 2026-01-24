using System;

namespace DataVisit
{
    //在生成TypeID Enum上标记对应的类型，用于重命名后生成代码时对应原有的TypeID
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class VisitTypeTagAttribute : Attribute
    {
        public Type Tag;
        public VisitTypeTagAttribute(Type tag) 
        {
            Tag = tag;
        }
    }
}
