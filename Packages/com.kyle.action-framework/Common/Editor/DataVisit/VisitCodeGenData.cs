using System;
using System.Collections.Generic;

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

        public Dictionary<Type, int> TypeIDs = new Dictionary<Type, int>();
        public readonly List<TypeData> Types = new List<TypeData>();
    }

    public class TypeData
    {
        public Type Type;
        public TypeData Base;
        public bool IsStruct;
        public readonly List<FieldData> Fields = new List<FieldData>();
        public int TypeId;
        public string TypeName => Type.Name;
        public string FullTypeName => Type.FullName;
    }

    public class FieldData
    {
        public System.Reflection.FieldInfo Field;
        public bool IsCollections;
        public TypeData CustomFieldType;
        public bool IsDynamic;
        public int FieldIndex;
        public uint Tag;
        
        public string FieldName => Field.Name;
        public Type FieldType => Field.FieldType;
    }

}
