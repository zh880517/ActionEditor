using System;
namespace DataVisit
{
    //字段标记，用于代码生成时指定字段索引和Tag
    //仅支持pulic字段
    [AttributeUsage(AttributeTargets.Field)]
    public class VisitFieldAttribute : Attribute
    {
        public int FieldIndex {  get; private set; }
        public uint Tag { get; private set; }

        public VisitFieldAttribute(int fieldIndex, uint tag = 0)
        {
            FieldIndex = fieldIndex;
            Tag = tag;
        }
    }

    //动态类型字段标记，支持容器类型字段
    public class VisitDynamicFieldAttribute : VisitFieldAttribute
    {
        public VisitDynamicFieldAttribute(int fieldIndex, uint tag = 0) : base(fieldIndex, tag)
        {
        }
    }
}