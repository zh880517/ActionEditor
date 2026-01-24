using System;
using System.Collections.Generic;
using DataVisit;

namespace CodeGen.DataVisit
{
    public class CatalogData
    {
        public Type AttributeType;
        public byte TypeIDFieldIndex;
        public string NameSpace;
        public string GeneratePath;
        public string GenTypeName;
        public int NextTypeID;

        public VisitCatalogAttribute CatalogAttribute;
        public Dictionary<Type, int> ExistTypeIDs = new Dictionary<Type, int>();
        public List<TypeData> Types = new List<TypeData>();
    }

    public class TypeData
    {
        public Type Type;
        public TypeData Base;
        public bool IsStruct;
        public bool IsDynamicType;
        public List<FieldData> Fields = new List<FieldData>();
        public Type BaseType;
        public int TypeId;
        public string TypeName => Type.Name;
        public string FullTypeName => Type.FullName;
    }

    public class FieldData
    {
        public System.Reflection.FieldInfo Field;
        public bool IsCollections;
        public TypeData CustomFieldType;
        public VisitFieldAttribute FieldAttribute;
        public bool IsDynamic;
        public int FieldIndex;
        public uint Tag;
        
        public string FieldName => Field.Name;
        public Type FieldType => Field.FieldType;
    }

    public class DynamicTypeInfo
    {
        public Type BaseType;
        public List<DynamicChildType> ChildTypes = new List<DynamicChildType>();
    }

    public class DynamicChildType
    {
        public Type Type;
        public int TypeId;
        public Type ExistingEnumField;
    }
}
