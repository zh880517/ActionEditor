using System;

namespace DataVisit
{
    //在生成TypeID Enum上标记对应的类型，用于重命名后生成代码时对应原有的TypeID
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class VisitTypeTagAttribute : Attribute
    {
        public Type TagType;
        public VisitTypeTagAttribute(Type tag) 
        {
            TagType = tag;
        }
    }

    //用来表示某个Enum是某个Catalog生成的TypeID Enum
    [AttributeUsage(AttributeTargets.Enum)]
    public class VisitTypeIDCatalogAttribute : Attribute
    {
        public Type CatalogType;
        public VisitTypeIDCatalogAttribute(Type catalogType)
        {
            CatalogType = catalogType;
        }
    }
}
