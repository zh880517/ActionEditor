using System;
namespace DataVisit
{
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
}