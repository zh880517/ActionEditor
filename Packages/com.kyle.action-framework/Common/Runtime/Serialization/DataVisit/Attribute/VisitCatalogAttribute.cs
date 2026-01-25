using System;

namespace DataVisit
{
    //使用时需继承该类，然后对所属模块的类型进行标记
    [AttributeUsage(AttributeTargets.Class| AttributeTargets.Struct, Inherited = true)]
    public abstract class VisitCatalogAttribute : Attribute
    {
        public abstract byte TypeIDFieldIndex { get; } //多态类型ID字段索引,最终生成的TypeId = (内部TypeID << 8) | TypeIDFieldValue
        public abstract string NameSpace { get; }
        public abstract string GeneratePath { get;}
    }
}
