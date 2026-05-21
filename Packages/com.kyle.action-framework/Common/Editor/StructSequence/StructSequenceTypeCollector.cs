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

            BuildOffsets(catalogs);
            return catalogs;
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
                    structData.Fields.Add(new SSFieldData
                    {
                        Field = field,
                        IsUnmanaged = fieldUnmanaged,
                        UnmanagedSize = fieldUnmanaged ? GetUnmanagedSize(field.FieldType) : 0,
                    });
                }
            }
            return structData;
        }

        // 计算所有非 unmanaged 结构体每个字段的 ByteOffset（紧凑顺序，无对齐填充）
        private static void BuildOffsets(List<SSCatalogData> catalogs)
        {
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
                        // unmanaged 字段按其实际大小，managed 引用字段存 ref index（int = 4字节）
                        offset += field.IsUnmanaged ? field.UnmanagedSize : sizeof(int);
                    }
                }
            }
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
