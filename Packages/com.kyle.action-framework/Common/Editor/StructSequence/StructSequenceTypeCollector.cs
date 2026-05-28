using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CodeGen.StructSequence
{
    public static class StructSequenceTypeCollector
    {
        // 扫描所有程序集，收集所有带有 StructSequenceCatalogAttribute 子类标记的结构体，按 Catalog 分组
        public static List<SSCatalogData> CollectAllCatalogs()
        {
            var catalogs = new List<SSCatalogData>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    // 发现 Catalog Attribute 子类，提前建立 Catalog 条目
                    if (type.IsSubclassOf(typeof(StructSequenceCatalogAttribute)))
                    {
                        if (!catalogs.Any(c => c.AttributeType == type))
                            catalogs.Add(CreateCatalog(type));
                        continue;
                    }

                    // 只处理结构体
                    if (!type.IsValueType)
                        continue;

                    var attr = type.GetCustomAttribute<StructSequenceCatalogAttribute>(true);
                    if (attr == null)
                        continue;

                    var catalog = catalogs.FirstOrDefault(c => c.AttributeType == attr.GetType());
                    if (catalog == null)
                    {
                        catalog = CreateCatalog(attr.GetType());
                        catalogs.Add(catalog);
                    }
                    catalog.Structs.Add(CreateStructData(type));
                }
            }

            ExpandNestedStructs(catalogs);
            BuildOffsets(catalogs);
            return catalogs;
        }

        // 从指定类型数组构建一个 SSCatalogData：过滤非 struct 类型（基本类型、枚举、指针均排除），并去重
        public static SSCatalogData CreateCatalogFromTypes(Type[] types, string nameSpace, string generatePath, string genClassName)
        {
            var catalog = new SSCatalogData
            {
                AttributeType = null,
                NameSpace = nameSpace,
                GeneratePath = generatePath,
                GenClassName = genClassName,
            };

            var seen = new HashSet<Type>();
            foreach (var type in types)
            {
                if (type == null)
                    continue;
                // 仅保留 struct（值类型，且非基本类型、非枚举、非指针）
                if (!type.IsValueType || type.IsPrimitive || type.IsEnum || type.IsPointer)
                    continue;
                if (!seen.Add(type))
                    continue;
                catalog.Structs.Add(CreateStructData(type));
            }

            var catalogs = new List<SSCatalogData> { catalog };
            ExpandNestedStructs(catalogs);
            BuildOffsets(catalogs);
            return catalog;
        }

        // 根据 Catalog Attribute 类型创建 SSCatalogData，推导生成类名
        private static SSCatalogData CreateCatalog(Type catalogAttributeType)
        {
            var instance = (StructSequenceCatalogAttribute)Activator.CreateInstance(catalogAttributeType);
            string name = catalogAttributeType.Name;
            if (name.EndsWith("Attribute"))
                name = name.Substring(0, name.Length - "Attribute".Length);
            if (name.EndsWith("Catalog"))
                name = name.Substring(0, name.Length - "Catalog".Length);

            return new SSCatalogData
            {
                AttributeType = catalogAttributeType,
                NameSpace = instance.NameSpace,
                GeneratePath = instance.GeneratePath,
                GenClassName = name + "StructSequenceRegister",
            };
        }

        // 分析一个结构体类型，构建 SSStructData
        private static SSStructData CreateStructData(Type type)
        {
            if (type.IsGenericType)
                throw new InvalidOperationException($"StructSequence does not support generic struct type {type.FullName}.");
            var structData = new SSStructData
            {
                Type = type,
                IsUnmanaged = IsUnmanagedType(type),
            };

            // 仅对非 unmanaged 结构体逐字段分析
            if (!structData.IsUnmanaged)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    bool fieldUnmanaged = IsUnmanagedType(field.FieldType);
                    bool fieldIsStruct = !fieldUnmanaged && field.FieldType.IsValueType;
                    var kind = fieldUnmanaged ? SSFieldKind.Unmanaged
                             : fieldIsStruct  ? SSFieldKind.Struct
                             :                  SSFieldKind.Reference;
                    structData.Fields.Add(new SSFieldData
                    {
                        Field = field,
                        Kind = kind,
                        UnmanagedSize = fieldUnmanaged ? GetUnmanagedSize(field.FieldType) : 0,
                    });
                }
            }
            return structData;
        }

        private static void ExpandNestedStructs(List<SSCatalogData> catalogs)
        {
            foreach (var catalog in catalogs)
            {
                var knownTypes = new HashSet<Type>(catalog.Structs.Select(s => s.Type));
                int index = 0;
                while (index < catalog.Structs.Count)
                {
                    var structData = catalog.Structs[index++];
                    foreach (var field in structData.Fields)
                    {
                        if (field.Kind != SSFieldKind.Struct)
                            continue;
                        if (knownTypes.Add(field.FieldType))
                            catalog.Structs.Add(CreateStructData(field.FieldType));
                    }
                }
            }
        }

        // 计算所有非 unmanaged 结构体每个字段的 ByteOffset（紧凑顺序，无对齐填充）
        private static void BuildOffsets(List<SSCatalogData> catalogs)
        {
            // 构建 Type → SSStructData 索引，供递归计算非 unmanaged struct 字段大小
            var structMap = new Dictionary<Type, SSStructData>();
            foreach (var catalog in catalogs)
                foreach (var s in catalog.Structs)
                    structMap[s.Type] = s;

            foreach (var catalog in catalogs)
            {
                foreach (var structData in catalog.Structs)
                {
                    if (structData.IsUnmanaged)
                        continue;
                    int offset = 0;
                    foreach (var field in structData.Fields)
                    {
                        field.ByteOffset = offset;
                        offset += GetFieldPayloadSize(field, structMap);
                    }
                }
            }
        }

        // 计算一个字段在 payload 中占用的字节数
        private static int GetFieldPayloadSize(SSFieldData field, Dictionary<Type, SSStructData> structMap)
        {
            switch (field.Kind)
            {
                case SSFieldKind.Unmanaged:
                    return field.UnmanagedSize;
                case SSFieldKind.Struct:
                    if (!structMap.TryGetValue(field.FieldType, out var nested))
                        throw new InvalidOperationException($"Nested struct {field.FieldType.FullName} was not collected for StructSequence.");
                    return ComputeStructPayloadSize(nested, structMap);
                default:
                    // 引用类型：存 ref index（int = 4字节）
                    return sizeof(int);
            }
        }

        // 递归计算一个非 unmanaged SSStructData 的 payload 总字节数
        private static int ComputeStructPayloadSize(SSStructData structData, Dictionary<Type, SSStructData> structMap)
        {
            if (structData.IsUnmanaged)
                return GetUnmanagedSize(structData.Type);
            int size = 0;
            foreach (var field in structData.Fields)
                size += GetFieldPayloadSize(field, structMap);
            return size;
        }

        // 判断一个类型是否为 unmanaged（值类型且所有字段递归均为 unmanaged）
        public static bool IsUnmanagedType(Type type)
        {
            if (!type.IsValueType) return false;
            if (type.IsPrimitive) return true;
            if (type.IsEnum) return true;
            if (type.IsPointer) return true;
            // 开放泛型值类型可能含引用字段
            if (type.IsGenericType && !type.IsGenericTypeDefinition) return false;

            // 递归检查所有实例字段（含 private，因为 struct 布局包含所有字段）
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (!IsUnmanagedType(field.FieldType))
                    return false;
            }
            return true;
        }

        // 获取 unmanaged 类型的字节大小（匹配 C# sizeof 语义）
        public static int GetUnmanagedSize(Type type)
        {
            // 原始类型硬编码，避免 Marshal.SizeOf 对 bool/char 的 P/Invoke 语义差异
            if (type == typeof(bool))    return 1;
            if (type == typeof(byte))    return 1;
            if (type == typeof(sbyte))   return 1;
            if (type == typeof(char))    return 2;
            if (type == typeof(short))   return 2;
            if (type == typeof(ushort))  return 2;
            if (type == typeof(int))     return 4;
            if (type == typeof(uint))    return 4;
            if (type == typeof(float))   return 4;
            if (type == typeof(long))    return 8;
            if (type == typeof(ulong))   return 8;
            if (type == typeof(double))  return 8;
            if (type == typeof(decimal)) return 16;
            if (type.IsEnum) return GetUnmanagedSize(Enum.GetUnderlyingType(type));
            if (type.IsPointer || type == typeof(IntPtr) || type == typeof(UIntPtr))
                return IntPtr.Size;
            // 嵌套 struct：使用 Marshal.SizeOf，与 unsafe context 的 sizeof(T) 一致
            try { return Marshal.SizeOf(type); }
            catch { return 0; }
        }
    }
}
