using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace EasyConfig.Editor
{
    public static class ExcelBinaryExportUtil
    {
        public static void ExportAll(string rootFolder, IConfigSerializer serializer)
        {
            var groups = ExcelBinaryTypeCollector.Collect();
            foreach (var group in groups)
            {
                ExportGroupData(rootFolder, serializer, group);
            }
        }

        public static void ExportGroup<TGroup>(string rootFolder, IConfigSerializer serializer)
            where TGroup : ConfigGroupAttribute
        {
            var groups = ExcelBinaryTypeCollector.Collect();
            foreach (var group in groups)
            {
                if (group.GroupAttributeType == typeof(TGroup))
                {
                    ExportGroupData(rootFolder, serializer, group);
                    return;
                }
            }
        }

        private static void ExportGroupData(string rootFolder, IConfigSerializer serializer, ConfigGroupData group)
        {
            foreach (var typeData in group.Types)
            {
                byte[] bytes = SerializeConfigData(typeData, serializer);
                if (bytes == null)
                    continue;

                string fileName = typeData.Type.Name.ToLowerInvariant() + ".bytes";
                string outputPath = Path.Combine(rootFolder, group.ExportSubFolder, fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, bytes);
            }
        }

        private static byte[] SerializeConfigData(ConfigTypeData typeData, IConfigSerializer serializer)
        {
            try
            {
                if (typeData.Kind == ConfigKind.List || typeData.Kind == ConfigKind.LinkedList)
                {
                    // typeof(ConfigListCollector<>).MakeGenericType(configType)
                    Type collectorType = typeof(ConfigListCollector<>).MakeGenericType(typeData.Type);
                    FieldInfo configsField = collectorType.GetField("Configs", BindingFlags.Public | BindingFlags.Static);
                    object configs = configsField.GetValue(null);

                    // Call serializer.Serialize<List<T>>(configs)
                    Type listType = typeof(List<>).MakeGenericType(typeData.Type);
                    MethodInfo serializeMethod = GetSerializeMethod(listType);
                    return (byte[])serializeMethod.Invoke(serializer, new object[] { configs });
                }
                else
                {
                    // typeof(ConfigDictionaryCollector<,>).MakeGenericType(keyType, configType)
                    Type collectorType = typeof(ConfigDictionaryCollector<,>).MakeGenericType(typeData.KeyType, typeData.Type);
                    FieldInfo configsField = collectorType.GetField("Configs", BindingFlags.Public | BindingFlags.Static);
                    object configs = configsField.GetValue(null);

                    // Call serializer.Serialize<Dictionary<TKey,T>>(configs)
                    Type dictType = typeof(Dictionary<,>).MakeGenericType(typeData.KeyType, typeData.Type);
                    MethodInfo serializeMethod = GetSerializeMethod(dictType);
                    return (byte[])serializeMethod.Invoke(serializer, new object[] { configs });
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ExcelBinaryExportUtil] Failed to serialize {typeData.Type.Name}: {ex}");
                return null;
            }
        }

        private static MethodInfo GetSerializeMethod(Type valueType)
        {
            // Find Serialize<T> on the interface definition then make generic with valueType
            MethodInfo openMethod = typeof(IConfigSerializer).GetMethod("Serialize");
            return openMethod.MakeGenericMethod(valueType);
        }
    }
}
