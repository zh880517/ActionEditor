using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyConfig.Editor
{
    public class ConfigTypeData
    {
        public Type Type;
        public bool IsListConfig;
        public Type KeyType;
    }

    public class ConfigGroupData
    {
        public Type GroupAttributeType;
        public string GeneratePath;
        public string Namespace;
        public string ClassName;
        public string ExportSubFolder;
        public List<ConfigTypeData> Types = new List<ConfigTypeData>();
    }

    public static class ConfigBinaryTypeCollector
    {
        private static readonly Type ListConfigOpenType = typeof(ListConfig<>);
        private static readonly Type DictionaryConfigOpenType = typeof(DictionaryConfig<,>);

        public static List<ConfigGroupData> Collect()
        {
            var groupMap = new Dictionary<Type, ConfigGroupData>();

            foreach (var type in TypeCollector<IConfig>.Types)
            {
                if (type.IsGenericTypeDefinition)
                    continue;

                ConfigGroupAttribute groupAttr = null;
                Type groupAttrType = null;
                int groupCount = 0;
                foreach (var attr in type.GetCustomAttributes(true))
                {
                    if (attr is ConfigGroupAttribute cga)
                    {
                        groupCount++;
                        groupAttr = cga;
                        groupAttrType = attr.GetType();
                    }
                }

                if (groupAttr == null)
                    continue;
                if (groupCount > 1)
                {
                    Debug.LogError($"[ConfigBinaryTypeCollector] Type {type.FullName} has multiple ConfigGroupAttribute annotations. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.GeneratePath))
                {
                    Debug.LogError($"[ConfigBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty GeneratePath. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.Namespace))
                {
                    Debug.LogError($"[ConfigBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty Namespace. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.ClassName))
                {
                    Debug.LogError($"[ConfigBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty ClassName. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.ExportSubFolder))
                {
                    Debug.LogError($"[ConfigBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty ExportSubFolder. Skipping.");
                    continue;
                }

                bool isListConfig = false;
                bool isDictConfig = false;
                Type keyType = null;

                var baseType = type.BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType)
                    {
                        var genericDef = baseType.GetGenericTypeDefinition();
                        if (genericDef == ListConfigOpenType)
                        {
                            var genericArg = baseType.GetGenericArguments()[0];
                            if (genericArg == type)
                                isListConfig = true;
                            else
                                Debug.LogWarning($"[ConfigBinaryTypeCollector] Type {type.FullName} extends ListConfig<{genericArg.Name}> but expected ListConfig<{type.Name}>.");
                            break;
                        }
                        else if (genericDef == DictionaryConfigOpenType)
                        {
                            var genericArgs = baseType.GetGenericArguments();
                            if (genericArgs[1] == type)
                            {
                                isDictConfig = true;
                                keyType = genericArgs[0];
                            }
                            else
                            {
                                Debug.LogWarning($"[ConfigBinaryTypeCollector] Type {type.FullName} extends DictionaryConfig<{genericArgs[0].Name}, {genericArgs[1].Name}> but expected DictionaryConfig<TKey, {type.Name}>.");
                            }
                            break;
                        }
                    }
                    baseType = baseType.BaseType;
                }

                if (!isListConfig && !isDictConfig)
                {
                    Debug.LogError($"[ConfigBinaryTypeCollector] Type {type.FullName} has ConfigGroupAttribute [{groupAttrType.Name}] but does not inherit ListConfig<{type.Name}> or DictionaryConfig<TKey, {type.Name}>. Skipping.");
                    continue;
                }

                if (!groupMap.TryGetValue(groupAttrType, out var groupData))
                {
                    groupData = new ConfigGroupData
                    {
                        GroupAttributeType = groupAttrType,
                        GeneratePath = groupAttr.GeneratePath,
                        Namespace = groupAttr.Namespace,
                        ClassName = groupAttr.ClassName,
                        ExportSubFolder = groupAttr.ExportSubFolder,
                    };
                    groupMap[groupAttrType] = groupData;
                }

                groupData.Types.Add(new ConfigTypeData
                {
                    Type = type,
                    IsListConfig = isListConfig,
                    KeyType = keyType,
                });
            }

            // Check for duplicate file names within each group
            foreach (var groupData in groupMap.Values)
            {
                var fileNames = new HashSet<string>();
                for (int i = groupData.Types.Count - 1; i >= 0; i--)
                {
                    string fileName = groupData.Types[i].Type.Name.ToLowerInvariant() + ".bytes";
                    if (!fileNames.Add(fileName))
                    {
                        Debug.LogError($"[ConfigBinaryTypeCollector] Duplicate config file name '{fileName}' in group [{groupData.GroupAttributeType.Name}] from type {groupData.Types[i].Type.FullName}. Skipping.");
                        groupData.Types.RemoveAt(i);
                    }
                }
            }

            return new List<ConfigGroupData>(groupMap.Values);
        }
    }
}
