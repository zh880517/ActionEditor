using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EasyConfig.Editor
{
    public enum ConfigKind
    {
        List,
        Dictionary,
        LinkedList,
        LinkedDictionary,
    }

    public class ConfigTypeData
    {
        public Type Type;
        public ConfigKind Kind;
        public Type KeyType;
        public Type PrimaryType;
        public string SheetName;

        public bool IsListConfig => Kind == ConfigKind.List || Kind == ConfigKind.LinkedList;
        public bool IsLinkedConfig => Kind == ConfigKind.LinkedList || Kind == ConfigKind.LinkedDictionary;
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

    public static class ExcelBinaryTypeCollector
    {
        private static readonly Type ListConfigOpenType = typeof(ListConfig<>);
        private static readonly Type DictionaryConfigOpenType = typeof(DictionaryConfig<,>);
        private static readonly Type LinkedListConfigOpenType = typeof(LinkedListConfig<,>);
        private static readonly Type LinkedDictionaryConfigOpenType = typeof(LinkedDictionaryConfig<,,>);

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
                    Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} has multiple ConfigGroupAttribute annotations. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.GeneratePath))
                {
                    Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty GeneratePath. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.Namespace))
                {
                    Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty Namespace. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.ClassName))
                {
                    Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty ClassName. Skipping.");
                    continue;
                }
                if (string.IsNullOrEmpty(groupAttr.ExportSubFolder))
                {
                    Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} group attribute {groupAttrType.Name} has empty ExportSubFolder. Skipping.");
                    continue;
                }

                if (!TryCreateTypeData(type, groupAttrType, out var typeData))
                {
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

                groupData.Types.Add(typeData);
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
                        Debug.LogError($"[ExcelBinaryTypeCollector] Duplicate config file name '{fileName}' in group [{groupData.GroupAttributeType.Name}] from type {groupData.Types[i].Type.FullName}. Skipping.");
                        groupData.Types.RemoveAt(i);
                    }
                }
            }

            return new List<ConfigGroupData>(groupMap.Values);
        }

        public static bool TryCreateTypeData(Type type, out ConfigTypeData typeData)
        {
            return TryCreateTypeData(type, null, out typeData);
        }

        private static bool TryCreateTypeData(Type type, Type groupAttrType, out ConfigTypeData typeData)
        {
            typeData = null;
            Type keyType = null;
            Type primaryType = null;
            ConfigKind? kind = null;

            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType)
                {
                    var genericDef = baseType.GetGenericTypeDefinition();
                    var genericArgs = baseType.GetGenericArguments();
                    if (genericDef == LinkedListConfigOpenType)
                    {
                        if (genericArgs[0] != type)
                        {
                            Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} extends LinkedListConfig<{genericArgs[0].Name}, {genericArgs[1].Name}> but expected LinkedListConfig<{type.Name}, TPrimary>. Skipping.");
                            return false;
                        }

                        primaryType = genericArgs[1];
                        if (!IsPrimaryListConfig(primaryType))
                        {
                            Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} linked primary type {primaryType.FullName} must inherit ListConfig<{primaryType.Name}>. Skipping.");
                            return false;
                        }

                        kind = ConfigKind.LinkedList;
                        break;
                    }
                    if (genericDef == LinkedDictionaryConfigOpenType)
                    {
                        if (genericArgs[1] != type)
                        {
                            Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} extends LinkedDictionaryConfig<{genericArgs[0].Name}, {genericArgs[1].Name}, {genericArgs[2].Name}> but expected LinkedDictionaryConfig<TKey, {type.Name}, TPrimary>. Skipping.");
                            return false;
                        }

                        keyType = genericArgs[0];
                        primaryType = genericArgs[2];
                        if (!IsPrimaryDictionaryConfig(primaryType, keyType))
                        {
                            Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} linked primary type {primaryType.FullName} must inherit DictionaryConfig<{keyType.Name}, {primaryType.Name}>. Skipping.");
                            return false;
                        }

                        kind = ConfigKind.LinkedDictionary;
                        break;
                    }
                    if (genericDef == ListConfigOpenType)
                    {
                        if (genericArgs[0] != type)
                        {
                            Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} extends ListConfig<{genericArgs[0].Name}> but expected ListConfig<{type.Name}>. Skipping.");
                            return false;
                        }

                        kind = ConfigKind.List;
                        break;
                    }
                    if (genericDef == DictionaryConfigOpenType)
                    {
                        if (genericArgs[1] != type)
                        {
                            Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName} extends DictionaryConfig<{genericArgs[0].Name}, {genericArgs[1].Name}> but expected DictionaryConfig<TKey, {type.Name}>. Skipping.");
                            return false;
                        }

                        keyType = genericArgs[0];
                        kind = ConfigKind.Dictionary;
                        break;
                    }
                }
                baseType = baseType.BaseType;
            }

            if (!kind.HasValue)
            {
                string groupMessage = groupAttrType == null ? string.Empty : $" has ConfigGroupAttribute [{groupAttrType.Name}] but";
                Debug.LogError($"[ExcelBinaryTypeCollector] Type {type.FullName}{groupMessage} does not inherit ListConfig<{type.Name}>, DictionaryConfig<TKey, {type.Name}>, LinkedListConfig<{type.Name}, TPrimary> or LinkedDictionaryConfig<TKey, {type.Name}, TPrimary>. Skipping.");
                return false;
            }

            typeData = new ConfigTypeData
            {
                Type = type,
                Kind = kind.Value,
                KeyType = keyType,
                PrimaryType = primaryType,
                SheetName = type.GetCustomAttribute<ExcelSheetAttribute>()?.Name,
            };
            return true;
        }

        private static bool IsPrimaryListConfig(Type primaryType)
        {
            return HasGenericConfigBase(primaryType, ListConfigOpenType, args => args[0] == primaryType);
        }

        private static bool IsPrimaryDictionaryConfig(Type primaryType, Type keyType)
        {
            return HasGenericConfigBase(primaryType, DictionaryConfigOpenType, args => args[0] == keyType && args[1] == primaryType);
        }

        private static bool HasGenericConfigBase(Type type, Type openType, Predicate<Type[]> match)
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == openType)
                {
                    return match(baseType.GetGenericArguments());
                }
                baseType = baseType.BaseType;
            }
            return false;
        }
    }
}
