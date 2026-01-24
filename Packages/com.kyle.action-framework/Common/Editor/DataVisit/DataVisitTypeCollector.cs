using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataVisit;
using UnityEditor.Build.Content;
using UnityEngine;

namespace CodeGen.DataVisit
{
    public static class DataVisitTypeCollector
    {
        private static CatalogData CreateCatalog(Type catalogType)
        {
            var catalogInfo = new CatalogData { AttributeType = catalogType };
            var instance = Activator.CreateInstance(catalogType) as VisitCatalogAttribute;
            catalogInfo.TypeIDFieldIndex = instance.TypeIDFieldIndex;
            catalogInfo.NameSpace = instance.NameSpace;
            catalogInfo.GeneratePath = instance.GeneratePath;
            catalogInfo.GenTypeName = catalogType.Name + "_Visit";

            return catalogInfo;
        }

        private static TypeData CreateTypeInfo(Type type)
        {
            var typeInfo = new TypeData
            {
                Type = type,
                IsStruct = type.IsValueType,
                IsDynamicType = type.GetCustomAttribute<VisitDynamicTypeAttribute>(true) != null,
            };
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var item in fields)
            {
                var data = CreateFieldInfo(item);
                if(data != null)
                    typeInfo.Fields.Add(data);
            }
            return typeInfo;
        }

        private static FieldData CreateFieldInfo(FieldInfo field)
        {
            var visitFieldAttr = field.GetCustomAttribute<VisitFieldAttribute>();
            if(visitFieldAttr == null)
                return null;
            return new FieldData
            { 
                Field = field,
                FieldIndex = visitFieldAttr.FieldIndex,
                Tag = visitFieldAttr.Tag,
                IsDynamic = field.FieldType.IsClass && field.GetCustomAttribute<VisitDynamicFieldAttribute>() != null
            };
        }

        private static void ReCalcNextTypeID(CatalogData catalog)
        {
            int maxTypeID = 0;
            foreach (var kv in catalog.ExistTypeIDs)
            {
                int typeID = kv.Value >> 8;
                if (typeID > maxTypeID)
                    maxTypeID = typeID;
            }
            catalog.NextTypeID = maxTypeID + 1;
        }

        public static List<CatalogData> CollectAllCatalogs()
        {
            List<CatalogData> catalogs = new List<CatalogData>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if(type.IsInterface || type.IsAbstract)
                        continue;
                    if(type.IsEnum)
                    {
                        var attr= type.GetCustomAttribute<VisitTypeIDCatalogAttribute>();
                        if (attr != null)
                        {
                            var catalog = catalogs.FirstOrDefault(c => c.AttributeType == typeof(VisitTypeIDCatalogAttribute));
                            catalog ??= CreateCatalog(typeof(VisitTypeIDCatalogAttribute));
                            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                            foreach (var field in fields)
                            {
                                var fieldAttr = field.GetCustomAttribute<VisitTypeTagAttribute>();
                                if(fieldAttr == null || fieldAttr.TagType == null)
                                    continue;
                                if(!catalog.ExistTypeIDs.TryGetValue(fieldAttr.TagType, out int exitID))
                                {
                                    catalog.ExistTypeIDs.Add(fieldAttr.TagType, exitID);
                                }
                                else
                                {
                                    Debug.LogError($"Duplicate TypeID for type {fieldAttr.TagType.FullName} in enum {type.FullName}");
                                }
                            }
                            ReCalcNextTypeID(catalog);
                        }
                    }
                    else if (type.IsSubclassOf(typeof(VisitCatalogAttribute)))
                    {
                        catalogs.Add(CreateCatalog(type));
                    }
                    else
                    {
                        var catalogAttr = type.GetCustomAttribute<VisitCatalogAttribute>(true);
                        if (catalogAttr != null)
                        {
                            var catalog = catalogs.FirstOrDefault(c => c.AttributeType == catalogAttr.GetType());
                            catalog ??= CreateCatalog(type);
                            var typeInfo = CreateTypeInfo(type);
                            catalog.Types.Add(typeInfo);
                        }
                    }
                }
            }
            if(!BuildCatalogs(catalogs))
            {
                return null;
            }
            return catalogs;
        }

        private static bool BuildCatalogs(List<CatalogData> catalogs)
        {
            foreach (var catalog in catalogs)
            {
                // 先处理基类关系
                int index = 0;
                while (index < catalog.Types.Count)
                {
                    var typeInfo = catalog.Types[index];
                    var baseType = typeInfo.Type.BaseType;
                    if (baseType != typeof(object))
                    {
                        typeInfo.Base = catalogs
                            .SelectMany(c => c.Types)
                            .FirstOrDefault(t => t.Type == baseType);
                        if(typeInfo.Base == null)
                        {
                            var baseTypeInfo = CreateTypeInfo(baseType);
                            catalog.Types.Add(baseTypeInfo);
                            typeInfo.Base = baseTypeInfo;
                        }
                    }

                }
                //分配动态类型ID
                foreach (var typeInfo in catalog.Types)
                {
                    //检查FieldIndex冲突
                    for (int i = 0; i < typeInfo.Fields.Count; i++)
                    {
                        var field = typeInfo.Fields[i];
                        var nextSameIndex = typeInfo.Fields.FindIndex(i + 1, f => f.FieldIndex == field.FieldIndex);
                        if (nextSameIndex != -1)
                        {
                            Debug.LogError($"Type {typeInfo.Type.FullName} has conflicting FieldIndex {field.FieldIndex} for fields {field.Field.Name} and {typeInfo.Fields[nextSameIndex].Field.Name}");
                            return false;
                        }
                    }
                    if (catalog.ExistTypeIDs.TryGetValue(typeInfo.Type, out int existID))
                    {
                        //如果已经存在ID，则继续使用已有ID，说明之前是动态类型
                        typeInfo.TypeId = existID;
                    }
                    else if (typeInfo.IsDynamicType)
                    {
                        if(catalog.TypeIDFieldIndex == 0)
                        {
                            //如果有动态类型，但没有指定TypeIDFieldIndex，则报错
                            Debug.LogError($"Catalog {catalog.AttributeType.FullName} has invalid TypeIDFieldIndex 0 for dynamic type {typeInfo.Type.FullName}");
                            return false;
                        }
                        int typeId = (catalog.NextTypeID << 8) | catalog.TypeIDFieldIndex;
                        catalog.ExistTypeIDs[typeInfo.Type] = typeId;
                        typeInfo.TypeId = typeId;
                        catalog.NextTypeID++;
                    }
                }
            }
            //检查TypeIDFieldIndex冲突
            for (int i = 0; i < catalogs.Count; i++)
            {
                var catalog = catalogs[i];
                if(catalog.NextTypeID > 1)
                {
                    int nextSameIndex =catalogs.FindIndex(i + 1, c => c.TypeIDFieldIndex == catalog.TypeIDFieldIndex);
                    if (nextSameIndex != -1)
                    {
                        Debug.LogError($"Catalog {catalog.AttributeType.FullName} has conflicting TypeIDFieldIndex {catalog.TypeIDFieldIndex} with Catalog {catalogs[nextSameIndex].AttributeType.FullName}");
                        return false;
                    }
                }
            }
            foreach (var typeInfo in catalogs)
            {
                foreach (var item in typeInfo.Types)
                {
                    foreach (var field in item.Fields)
                    {
                        Type realFieldType = field.FieldType;
                        if (field.FieldType.IsGenericType)
                        {
                            var genericDef = field.FieldType.GetGenericTypeDefinition();
                            if (genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>) || genericDef == typeof(HashSet<>))
                            {
                                field.IsCollections = true;
                                var genericArgs = field.FieldType.GetGenericArguments();
                                realFieldType = genericArgs.Length == 2 ? genericArgs[1] : genericArgs[0];
                            }
                        }
                        else if(field.FieldType.IsArray)
                        {
                            field.IsCollections = true;
                            realFieldType = field.FieldType.GetElementType();
                        }
                        field.CustomFieldType = catalogs
                            .SelectMany(c => c.Types)
                            .FirstOrDefault(t => t.Type == realFieldType);
                    }
                }
            }
            return true;
        }


        public static List<DynamicTypeInfo> CollectDynamicTypes(CatalogData catalog)
        {
            var dynamicTypes = new Dictionary<Type, DynamicTypeInfo>();
            
            foreach (var typeInfo in catalog.Types)
            {
                if (typeInfo.IsDynamicType && !dynamicTypes.ContainsKey(typeInfo.Type))
                {
                    dynamicTypes[typeInfo.Type] = new DynamicTypeInfo { BaseType = typeInfo.Type };
                }
            }
            
            int internalTypeId = 1;
            foreach (var typeInfo in catalog.Types)
            {
                if (typeInfo.BaseType != null)
                {
                    var dynamicBase = FindDynamicBaseType(typeInfo.Type, dynamicTypes.Keys);
                    if (dynamicBase != null && dynamicTypes.TryGetValue(dynamicBase, out var dynamicInfo))
                    {
                        int fieldValue = GetTypeIdFieldValue(typeInfo.Type, catalog.CatalogAttribute.TypeIDFieldIndex);
                        int typeId = (internalTypeId << 8) | fieldValue;
                        internalTypeId++;
                        
                        dynamicInfo.ChildTypes.Add(new DynamicChildType
                        {
                            Type = typeInfo.Type,
                            TypeId = typeId
                        });
                    }
                }
            }
            
            return dynamicTypes.Values.Where(d => d.ChildTypes.Count > 0).ToList();
        }

        private static Type FindDynamicBaseType(Type type, IEnumerable<Type> dynamicBases)
        {
            var current = type.BaseType;
            while (current != null && current != typeof(object))
            {
                if (dynamicBases.Contains(current)) return current;
                current = current.BaseType;
            }
            return null;
        }

        private static int GetTypeIdFieldValue(Type type, byte fieldIndex)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.IsStatic || field.IsLiteral)
                {
                    var fieldType = field.FieldType;
                    if (fieldType == typeof(int) || fieldType == typeof(uint) || 
                        fieldType == typeof(byte) || fieldType == typeof(sbyte) ||
                        fieldType == typeof(short) || fieldType == typeof(ushort))
                    {
                        var value = field.GetValue(null);
                        if (value != null) return Convert.ToInt32(value);
                    }
                }
            }
            
            return Math.Abs(type.Name.GetHashCode()) % 256;
        }
    }
}
