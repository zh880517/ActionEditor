using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataVisit;
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
            string name = catalogType.Name;
            if(name.EndsWith("Attribute"))
                name = name.Substring(0, name.Length - "Attribute".Length);
            if(name.EndsWith("Catalog"))
                name = name.Substring(0, name.Length - "Catalog".Length);
            catalogInfo.GenTypeName =name + "Visit";

            return catalogInfo;
        }

        private static TypeData CreateTypeInfo(Type type)
        {
            var typeInfo = new TypeData
            {
                Type = type,
                IsStruct = type.IsValueType,
            };
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var item in fields)
            {
                var data = CreateFieldInfo(item);
                if(data != null)
                    typeInfo.Fields.Add(data);
            }
            typeInfo.Fields.Sort((a, b) => a.FieldIndex.CompareTo(b.FieldIndex));
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
            foreach (var kv in catalog.TypeIDs)
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
                            var catalog = catalogs.FirstOrDefault(c => c.AttributeType == attr.CatalogType);
                            if (catalog == null)
                            {
                                catalog = CreateCatalog(attr.CatalogType);
                                catalogs.Add(catalog);
                            }
                            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                            foreach (var field in fields)
                            {
                                var fieldAttr = field.GetCustomAttribute<VisitTypeTagAttribute>();
                                if(fieldAttr == null || fieldAttr.TagType == null)
                                    continue;
                                int typeIdValue = (int)field.GetRawConstantValue();
                                if(!catalog.TypeIDs.TryGetValue(fieldAttr.TagType, out int existID))
                                {
                                    catalog.TypeIDs.Add(fieldAttr.TagType, typeIdValue);
                                }
                                else if(existID != typeIdValue)
                                {
                                    Debug.LogError($"Conflicting TypeID for type {fieldAttr.TagType.FullName} in enum {type.FullName}");
                                }
                            }
                        }
                    }
                    else if (type.IsSubclassOf(typeof(VisitCatalogAttribute)))
                    {
                        if (!catalogs.Any(c => c.AttributeType == type))
                            catalogs.Add(CreateCatalog(type));
                    }
                    else
                    {
                        var catalogAttr = type.GetCustomAttribute<VisitCatalogAttribute>(true);
                        if (catalogAttr != null)
                        {
                            var catalog = catalogs.FirstOrDefault(c => c.AttributeType == catalogAttr.GetType());
                            if (catalog == null)
                            {
                                catalog = CreateCatalog(catalogAttr.GetType());
                                catalogs.Add(catalog);
                            }
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
            // 预建类型查找字典，避免嵌套LINQ O(N²)
            var typeMap = new Dictionary<Type, TypeData>();
            foreach (var catalog in catalogs)
            {
                foreach (var t in catalog.Types)
                    typeMap[t.Type] = t;
            }

            foreach (var catalog in catalogs)
            {
                if(catalog.TypeIDFieldIndex == 0)
                {
                    Debug.LogError($"Catalog {catalog.AttributeType.FullName} has invalid TypeIDFieldIndex 0");
                    return false;
                }

                ReCalcNextTypeID(catalog);
                // 先处理基类关系
                int index = 0;
                while (index < catalog.Types.Count)
                {
                    var typeInfo = catalog.Types[index];
                    var baseType = typeInfo.Type.BaseType;
                    if (!typeInfo.IsStruct && baseType != typeof(object))
                    {
                        typeMap.TryGetValue(baseType, out typeInfo.Base);
                        if(typeInfo.Base == null)
                        {
                            var baseTypeInfo = CreateTypeInfo(baseType);
                            catalog.Types.Add(baseTypeInfo);
                            typeMap[baseType] = baseTypeInfo;
                            typeInfo.Base = baseTypeInfo;
                        }
                    }
                    index++;
                }
                //分配动态类型ID
                foreach (var typeInfo in catalog.Types)
                {
                    if (!typeInfo.IsStruct && !IsNewableClass(typeInfo.Type))
                    {
                        Debug.LogError($"Type {typeInfo.Type.FullName} must be a non-abstract class with a public parameterless constructor for DataVisit generation");
                        return false;
                    }
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
                        if (!ValidateFieldConstraints(typeInfo.Type, field))
                            return false;
                    }
                    if(!typeInfo.IsStruct)
                    {
                        if (catalog.TypeIDs.TryGetValue(typeInfo.Type, out int existID))
                        {
                            //如果已经存在ID，则继续使用已有ID
                            typeInfo.TypeId = existID;
                        }
                        else
                        {
                            int typeId = (catalog.NextTypeID << 8) | catalog.TypeIDFieldIndex;
                            typeInfo.TypeId = typeId;
                            catalog.TypeIDs[typeInfo.Type] = typeId;
                            catalog.NextTypeID++;
                        }
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
                        field.CustomFieldType = typeMap.GetValueOrDefault(realFieldType);
                    }
                }
            }
            return true;
        }

        private static bool ValidateFieldConstraints(Type ownerType, FieldData field)
        {
            Type fieldType = field.FieldType;
            if (fieldType.IsInterface)
            {
                Debug.LogError($"Type {ownerType.FullName} field {field.Field.Name} uses unsupported interface field type {fieldType.FullName}");
                return false;
            }

            bool containerType = fieldType.IsArray || fieldType.IsGenericType;
            if (containerType && !ValidateContainerElementConstraints(ownerType, field))
                return false;
            if (fieldType.IsClass && fieldType != typeof(string) && !containerType && !IsNewableClass(fieldType))
            {
                Debug.LogError($"Type {ownerType.FullName} field {field.Field.Name} type {fieldType.FullName} must be a non-abstract class with a public parameterless constructor");
                return false;
            }

            if (!field.IsDynamic)
                return true;

            Type dynamicType = fieldType;
            if (fieldType.IsArray)
                dynamicType = fieldType.GetElementType();
            else if (fieldType.IsGenericType)
            {
                var args = fieldType.GetGenericArguments();
                dynamicType = args.Length == 2 ? args[1] : args[0];
            }

            if (dynamicType == null || !IsNewableClass(dynamicType))
            {
                Debug.LogError($"Type {ownerType.FullName} dynamic field {field.Field.Name} element type must be a non-abstract class with a public parameterless constructor");
                return false;
            }
            return true;
        }

        private static bool ValidateContainerElementConstraints(Type ownerType, FieldData field)
        {
            foreach (var elementType in GetContainerElementTypes(field.FieldType))
            {
                if (elementType == null || elementType == typeof(string))
                    continue;
                if (elementType.IsInterface)
                {
                    Debug.LogError($"Type {ownerType.FullName} field {field.Field.Name} uses unsupported interface container element type {elementType.FullName}");
                    return false;
                }
                if (elementType.IsClass && !IsNewableClass(elementType))
                {
                    Debug.LogError($"Type {ownerType.FullName} field {field.Field.Name} container element type {elementType.FullName} must be a non-abstract class with a public parameterless constructor");
                    return false;
                }
            }
            return true;
        }

        private static IEnumerable<Type> GetContainerElementTypes(Type fieldType)
        {
            if (fieldType.IsArray)
            {
                yield return fieldType.GetElementType();
                yield break;
            }
            if (!fieldType.IsGenericType)
                yield break;
            var genericDef = fieldType.GetGenericTypeDefinition();
            var args = fieldType.GetGenericArguments();
            if (genericDef == typeof(Dictionary<,>))
            {
                yield return args[0];
                yield return args[1];
            }
            else
            {
                yield return args[0];
            }
        }

        private static bool IsNewableClass(Type type)
        {
            return type != null
                && type.IsClass
                && !type.IsAbstract
                && type.GetConstructor(Type.EmptyTypes) != null;
        }

    }
}
