using System.Collections.Generic;
using System.IO;
using CodeGen;
using UnityEditor;

namespace CodeGen.StructSequence
{
    public static class StructSequenceCodeGenerator
    {
        [MenuItem("Tools/StructSequence/Generate All", priority = 100)]
        public static void GenerateAll()
        {
            var catalogs = StructSequenceTypeCollector.CollectAllCatalogs();
            if (catalogs.Count == 0)
                return;

            var modifiedFiles = new HashSet<string>();
            foreach (var catalog in catalogs)
            {
                if (catalog.Structs.Count == 0)
                    continue;
                GenerateCatalogFiles(catalog, modifiedFiles);
            }

            if (modifiedFiles.Count > 0)
                AssetDatabase.Refresh();
        }

        // 为一个 Catalog 生成三个 partial class 文件：Write / Read / 注册
        private static void GenerateCatalogFiles(SSCatalogData catalog, HashSet<string> modifiedFiles)
        {
            string path;

            path = GenerateWriteFile(catalog);
            if (path != null) modifiedFiles.Add(path);

            path = GenerateReadFile(catalog);
            if (path != null) modifiedFiles.Add(path);

            path = GenerateRegisterFile(catalog);
            if (path != null) modifiedFiles.Add(path);
        }

        // 生成 {GenClassName}_Write.cs，包含所有 non-unmanaged struct 的 Write 方法
        private static string GenerateWriteFile(SSCatalogData catalog)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//此文件由工具自动生成: StructSequenceCodeGenerator.GenerateAll()");

            using (new CSharpCodeWriter.NameSpaceScop(writer, catalog.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public static unsafe partial class {catalog.GenClassName}"))
                {
                    bool hasMethod = false;
                    foreach (var structData in catalog.Structs)
                    {
                        if (structData.IsUnmanaged)
                            continue;
                        if (hasMethod)
                            writer.NewLine();
                        GenerateWriteMethod(writer, structData, catalog.NameSpace);
                        hasMethod = true;
                    }
                }
            }

            string filePath = Path.Combine(catalog.GeneratePath, $"{catalog.GenClassName}_Write.cs");
            if (GeneratorUtils.WriteToFile(filePath, writer.ToString()))
                return filePath;
            return null;
        }

        // 生成 {GenClassName}_Read.cs，包含所有 non-unmanaged struct 的 Read 方法
        private static string GenerateReadFile(SSCatalogData catalog)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//此文件由工具自动生成: StructSequenceCodeGenerator.GenerateAll()");

            using (new CSharpCodeWriter.NameSpaceScop(writer, catalog.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public static unsafe partial class {catalog.GenClassName}"))
                {
                    bool hasMethod = false;
                    foreach (var structData in catalog.Structs)
                    {
                        if (structData.IsUnmanaged)
                            continue;
                        if (hasMethod)
                            writer.NewLine();
                        GenerateReadMethod(writer, structData, catalog.NameSpace);
                        hasMethod = true;
                    }
                }
            }

            string filePath = Path.Combine(catalog.GeneratePath, $"{catalog.GenClassName}_Read.cs");
            if (GeneratorUtils.WriteToFile(filePath, writer.ToString()))
                return filePath;
            return null;
        }

        // 生成 {GenClassName}.cs，包含 Init() 注册方法
        private static string GenerateRegisterFile(SSCatalogData catalog)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("using UnityEngine;");
            writer.WriteLine("//此文件由工具自动生成: StructSequenceCodeGenerator.GenerateAll()");

            using (new CSharpCodeWriter.NameSpaceScop(writer, catalog.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public static unsafe partial class {catalog.GenClassName}"))
                {
                    writer.WriteLine("[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
                    using (new CSharpCodeWriter.Scop(writer, "public static void Init()"))
                    {
                        foreach (var structData in catalog.Structs)
                        {
                            GenerateRegistration(writer, structData, catalog.NameSpace);
                        }
                    }
                }
            }

            string filePath = Path.Combine(catalog.GeneratePath, $"{catalog.GenClassName}.cs");
            if (GeneratorUtils.WriteToFile(filePath, writer.ToString()))
                return filePath;
            return null;
        }

        // 生成单个结构体的 Write 方法
        private static void GenerateWriteMethod(CSharpCodeWriter writer, SSStructData structData, string nameSpace)
        {
            string typeName = GeneratorUtils.TypeToName(structData.Type, nameSpace);
            string sig = $"private static void Write{structData.TypeName}(InternalSequence block, byte* ptr, ref {typeName} value)";
            using (new CSharpCodeWriter.Scop(writer, sig))
            {
                foreach (var field in structData.Fields)
                {
                    if (field.IsUnmanaged)
                    {
                        string fieldTypeName = GeneratorUtils.TypeToName(field.FieldType, nameSpace);
                        writer.WriteLine($"*({fieldTypeName}*)(ptr + {field.ByteOffset}) = value.{field.FieldName};");
                    }
                    else
                    {
                        writer.WriteLine($"*(int*)(ptr + {field.ByteOffset}) = block.WriteRef(value.{field.FieldName});");
                    }
                }
            }
        }

        // 生成单个结构体的 Read 方法
        private static void GenerateReadMethod(CSharpCodeWriter writer, SSStructData structData, string nameSpace)
        {
            string typeName = GeneratorUtils.TypeToName(structData.Type, nameSpace);
            string sig = $"private static {typeName} Read{structData.TypeName}(InternalSequence block, byte* ptr)";
            using (new CSharpCodeWriter.Scop(writer, sig))
            {
                writer.WriteLine($"{typeName} data = default;");
                foreach (var field in structData.Fields)
                {
                    if (field.IsUnmanaged)
                    {
                        string fieldTypeName = GeneratorUtils.TypeToName(field.FieldType, nameSpace);
                        writer.WriteLine($"data.{field.FieldName} = *({fieldTypeName}*)(ptr + {field.ByteOffset});");
                    }
                    else
                    {
                        string fieldTypeName = GeneratorUtils.TypeToName(field.FieldType, nameSpace);
                        writer.WriteLine($"data.{field.FieldName} = ({fieldTypeName})block.GetRef(*(int*)(ptr + {field.ByteOffset}));");
                    }
                }
                writer.WriteLine("return data;");
            }
        }

        // 生成 Init() 中对单个结构体的注册代码
        private static void GenerateRegistration(CSharpCodeWriter writer, SSStructData structData, string nameSpace)
        {
            string typeName = GeneratorUtils.TypeToName(structData.Type, nameSpace);
            if (structData.IsUnmanaged)
            {
                writer.WriteLine($"UnsafeStructAccessor<{typeName}>.Init(UnmanagedStructAccessor<{typeName}>.Size, UnmanagedStructAccessor<{typeName}>.Write, UnmanagedStructAccessor<{typeName}>.Read);");
            }
            else
            {
                int totalSize = ComputePayloadSize(structData);
                writer.WriteLine($"UnsafeStructAccessor<{typeName}>.Init({totalSize}, Write{structData.TypeName}, Read{structData.TypeName});");
            }
        }

        // 计算非 unmanaged 结构体 payload 的总字节数（紧凑布局）
        private static int ComputePayloadSize(SSStructData structData)
        {
            int size = 0;
            foreach (var field in structData.Fields)
                size += field.IsUnmanaged ? field.UnmanagedSize : sizeof(int);
            return size;
        }
    }
}
