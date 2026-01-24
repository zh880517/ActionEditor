using System;

namespace DataVisit
{
    //使用时需继承该类，然后对所属模块的类型进行标记
    [AttributeUsage(AttributeTargets.Class| AttributeTargets.Struct)]
    public class VisitCatalogAttribute : Attribute
    {
        public byte TypeIDFieldIndex { get; set; } //多态类型ID字段索引,最终生成的TypeId = (内部TypeID << 8) | TypeIDFieldValue
        public string CatalogName { get; private set; }
        public string NameSpace { get; private set; }
        public string GeneratePath { get; private set; }
        public VisitCatalogAttribute(byte idFieldIndex, string catalogName, string nameSpace, string generatePath)
        {
            TypeIDFieldIndex = idFieldIndex;
            CatalogName = catalogName;
            NameSpace = nameSpace;
            GeneratePath = generatePath;
        }
    }
}
