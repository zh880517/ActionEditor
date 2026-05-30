using UnityEngine;

namespace EasyConfig.Editor
{
    internal static class EditorExcelConfigLinkUtility
    {
        public static void LinkPrimary(ConfigTypeData typeData)
        {
            if (typeData == null || !typeData.IsLinkedConfig)
                return;

            if (typeData.Kind == ConfigKind.LinkedList)
            {
                LinkLinkedList(typeData);
                return;
            }

            LinkLinkedDictionary(typeData);
        }

        private static void LinkLinkedList(ConfigTypeData typeData)
        {
            var method = typeof(EditorExcelConfigLinkUtility).GetMethod(nameof(LinkLinkedListGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.MakeGenericMethod(typeData.Type, typeData.PrimaryType).Invoke(null, null);
        }

        private static void LinkLinkedDictionary(ConfigTypeData typeData)
        {
            var method = typeof(EditorExcelConfigLinkUtility).GetMethod(nameof(LinkLinkedDictionaryGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.MakeGenericMethod(typeData.KeyType, typeData.Type, typeData.PrimaryType).Invoke(null, null);
        }

        private static void LinkLinkedListGeneric<TLinked, TPrimary>()
            where TLinked : LinkedListConfig<TLinked, TPrimary>
            where TPrimary : IListConfig
        {
            var linkedConfigs = ConfigListCollector<TLinked>.Configs;
            var primaryConfigs = ConfigListCollector<TPrimary>.Configs;
            for (int i = 0; i < linkedConfigs.Count; i++)
            {
                if (i < primaryConfigs.Count)
                {
                    LinkedListConfig<TLinked, TPrimary>.LinkPrimary(linkedConfigs[i], primaryConfigs[i]);
                }
                else
                {
                    Debug.LogError($"[EditorExcelConfigLinkUtility] 关联配置 {typeof(TLinked).Name} 第 {i} 行缺少主配置 {typeof(TPrimary).Name}。");
                }
            }
        }

        private static void LinkLinkedDictionaryGeneric<TKey, TLinked, TPrimary>()
            where TKey : struct
            where TLinked : LinkedDictionaryConfig<TKey, TLinked, TPrimary>
            where TPrimary : IDictionaryConfig
        {
            var linkedConfigs = ConfigDictionaryCollector<TKey, TLinked>.Configs;
            var primaryConfigs = ConfigDictionaryCollector<TKey, TPrimary>.Configs;
            foreach (var kv in linkedConfigs)
            {
                if (primaryConfigs.TryGetValue(kv.Key, out var primary))
                {
                    LinkedDictionaryConfig<TKey, TLinked, TPrimary>.LinkPrimary(kv.Value, primary);
                }
                else
                {
                    Debug.LogError($"[EditorExcelConfigLinkUtility] 关联配置 {typeof(TLinked).Name} Key={kv.Key} 缺少主配置 {typeof(TPrimary).Name}。");
                }
            }
        }
    }
}
