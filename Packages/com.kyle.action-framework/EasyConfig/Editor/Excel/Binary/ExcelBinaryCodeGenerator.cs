using System.IO;
using System.Linq;
using CodeGen;
using UnityEditor;

namespace EasyConfig.Editor
{
    public static class ExcelBinaryCodeGenerator
    {
        public static void GenerateAll()
        {
            var groups = ExcelBinaryTypeCollector.Collect();
            bool anyWritten = false;
            foreach (var group in groups)
            {
                bool written = GenerateGroup(group);
                if (written)
                    anyWritten = true;
            }
            if (anyWritten)
                AssetDatabase.Refresh();
        }

        private static bool GenerateGroup(ConfigGroupData group)
        {
            var writer = new CSharpCodeWriter(editorable: true);

            writer.WriteLine("// Auto-generated — do not edit manually.");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using EasyConfig;");

            using (new CSharpCodeWriter.NameSpaceScop(writer, group.Namespace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public static class {group.ClassName}"))
                {
                    GenerateLoadAll(writer, group);
                    if (group.Types.Any(it => it.IsLinkedConfig))
                    {
                        writer.NewLine();
                        GenerateLinkAll(writer, group);
                    }
                    writer.NewLine();
                    GenerateClear(writer, group);
                }
            }

            string filePath = Path.Combine(group.GeneratePath, group.ClassName + ".cs");
            return GeneratorUtils.WriteToFile(filePath, writer.ToString());
        }

        private static void GenerateLoadAll(CSharpCodeWriter writer, ConfigGroupData group)
        {
            using (new CSharpCodeWriter.Scop(writer, "public static void LoadAll(IConfigBytesProvider provider, IConfigSerializer serializer)"))
            {
                foreach (var typeData in group.Types)
                {
                    string typeName = GeneratorUtils.TypeToName(typeData.Type, group.Namespace);
                    string fileName = typeData.Type.Name.ToLowerInvariant() + ".bytes";
                    string groupName = group.ExportSubFolder;

                    using (new CSharpCodeWriter.Scop(writer))
                    {
                        writer.WriteLine($"var bytes = provider.LoadBytes(\"{groupName}\", \"{fileName}\");");
                        if (typeData.IsListConfig)
                        {
                            writer.WriteLine($"var list = serializer.Deserialize<List<{typeName}>>(bytes);");
                            writer.WriteLine($"ConfigListCollector<{typeName}>.Configs.Clear();");
                            writer.WriteLine($"ConfigListCollector<{typeName}>.Configs.AddRange(list);");
                        }
                        else
                        {
                            string keyTypeName = GeneratorUtils.TypeToName(typeData.KeyType, group.Namespace);
                            writer.WriteLine($"var dict = serializer.Deserialize<Dictionary<{keyTypeName}, {typeName}>>(bytes);");
                            writer.WriteLine($"ConfigDictionaryCollector<{keyTypeName}, {typeName}>.Configs.Clear();");
                            using (new CSharpCodeWriter.Scop(writer, "foreach (var kv in dict)"))
                            {
                                writer.WriteLine($"ConfigDictionaryCollector<{keyTypeName}, {typeName}>.Configs.Add(kv.Key, kv.Value);");
                            }
                        }
                    }
                }

                if (group.Types.Any(it => it.IsLinkedConfig))
                {
                    writer.WriteLine("LinkAll();");
                }
            }
        }

        private static void GenerateLinkAll(CSharpCodeWriter writer, ConfigGroupData group)
        {
            using (new CSharpCodeWriter.Scop(writer, "private static void LinkAll()"))
            {
                foreach (var typeData in group.Types)
                {
                    if (!typeData.IsLinkedConfig)
                        continue;

                    if (typeData.Kind == ConfigKind.LinkedList)
                    {
                        GenerateLinkedListLink(writer, group, typeData);
                    }
                    else if (typeData.Kind == ConfigKind.LinkedDictionary)
                    {
                        GenerateLinkedDictionaryLink(writer, group, typeData);
                    }
                }
            }
        }

        private static void GenerateLinkedListLink(CSharpCodeWriter writer, ConfigGroupData group, ConfigTypeData typeData)
        {
            string typeName = GeneratorUtils.TypeToName(typeData.Type, group.Namespace);
            string primaryTypeName = GeneratorUtils.TypeToName(typeData.PrimaryType, group.Namespace);

            using (new CSharpCodeWriter.Scop(writer))
            {
                writer.WriteLine($"var linkedConfigs = ConfigListCollector<{typeName}>.Configs;");
                writer.WriteLine($"var primaryConfigs = ConfigListCollector<{primaryTypeName}>.Configs;");
                using (new CSharpCodeWriter.Scop(writer, "for (int i = 0; i < linkedConfigs.Count; i++)"))
                {
                    using (new CSharpCodeWriter.Scop(writer, "if (i < primaryConfigs.Count)"))
                    {
                        writer.WriteLine($"LinkedListConfig<{typeName}, {primaryTypeName}>.LinkPrimary(linkedConfigs[i], primaryConfigs[i]);");
                    }
                    using (new CSharpCodeWriter.Scop(writer, "else"))
                    {
                        writer.WriteLine($"UnityEngine.Debug.LogError($\"[ExcelBinaryCodeGenerator] 关联配置 {typeName} 第 {{i}} 行缺少主配置 {primaryTypeName}。\");");
                    }
                }
            }
        }

        private static void GenerateLinkedDictionaryLink(CSharpCodeWriter writer, ConfigGroupData group, ConfigTypeData typeData)
        {
            string typeName = GeneratorUtils.TypeToName(typeData.Type, group.Namespace);
            string keyTypeName = GeneratorUtils.TypeToName(typeData.KeyType, group.Namespace);
            string primaryTypeName = GeneratorUtils.TypeToName(typeData.PrimaryType, group.Namespace);

            using (new CSharpCodeWriter.Scop(writer))
            {
                writer.WriteLine($"var linkedConfigs = ConfigDictionaryCollector<{keyTypeName}, {typeName}>.Configs;");
                writer.WriteLine($"var primaryConfigs = ConfigDictionaryCollector<{keyTypeName}, {primaryTypeName}>.Configs;");
                using (new CSharpCodeWriter.Scop(writer, "foreach (var kv in linkedConfigs)"))
                {
                    using (new CSharpCodeWriter.Scop(writer, "if (primaryConfigs.TryGetValue(kv.Key, out var primary))"))
                    {
                        writer.WriteLine($"LinkedDictionaryConfig<{keyTypeName}, {typeName}, {primaryTypeName}>.LinkPrimary(kv.Value, primary);");
                    }
                    using (new CSharpCodeWriter.Scop(writer, "else"))
                    {
                        writer.WriteLine($"UnityEngine.Debug.LogError($\"[ExcelBinaryCodeGenerator] 关联配置 {typeName} Key={{kv.Key}} 缺少主配置 {primaryTypeName}。\");");
                    }
                }
            }
        }

        private static void GenerateClear(CSharpCodeWriter writer, ConfigGroupData group)
        {
            using (new CSharpCodeWriter.Scop(writer, "public static void Clear()"))
            {
                foreach (var typeData in group.Types)
                {
                    string typeName = GeneratorUtils.TypeToName(typeData.Type, group.Namespace);
                    if (typeData.IsListConfig)
                    {
                        writer.WriteLine($"ConfigListCollector<{typeName}>.Configs.Clear();");
                    }
                    else
                    {
                        string keyTypeName = GeneratorUtils.TypeToName(typeData.KeyType, group.Namespace);
                        writer.WriteLine($"ConfigDictionaryCollector<{keyTypeName}, {typeName}>.Configs.Clear();");
                    }
                }
            }
        }
    }
}
