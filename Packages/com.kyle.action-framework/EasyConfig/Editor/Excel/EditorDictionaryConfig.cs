using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyConfig.Editor
{
    public static class EditorDictionaryConfig<T> where T : IDictionaryConfig
    {
        private static bool loaded;
        private static ConfigTypeData typeData;
        private static IEditorDictionaryConfigAccessor<T> accessor;

        public static int Count
        {
            get
            {
                EnsureLoaded();
                return accessor == null ? 0 : accessor.Count;
            }
        }

        public static IReadOnlyDictionary<object, T> Configs
        {
            get
            {
                EnsureLoaded();
                return accessor == null ? EmptyConfigs.Value : accessor.Configs;
            }
        }

        public static T Get(object key)
        {
            EnsureLoaded();
            return accessor == null ? default : accessor.Get(key);
        }

        public static bool Contains(object key)
        {
            EnsureLoaded();
            return accessor != null && accessor.Contains(key);
        }

        private static void EnsureLoaded()
        {
            if (loaded)
                return;
            if (!TryGetTypeData(out var data))
                return;

            try
            {
                accessor.ReadFromCache();
                EditorExcelConfigLinkUtility.LinkPrimary(data);
                EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(T));
                loaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorDictionaryConfig] 读取 Editor Excel 缓存失败：{typeof(T).FullName}\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        private static bool TryGetTypeData(out ConfigTypeData data)
        {
            if (typeData != null)
            {
                data = typeData;
                return true;
            }

            data = null;
            if (!ExcelBinaryTypeCollector.TryCreateTypeData(typeof(T), out var result))
                return false;
            if (result.Kind != ConfigKind.Dictionary && result.Kind != ConfigKind.LinkedDictionary)
            {
                Debug.LogError($"[EditorDictionaryConfig] 类型 {typeof(T).FullName} 不是 Dictionary 或 LinkedDictionary 配置，不能通过 EditorDictionaryConfig 访问。");
                return false;
            }
            if (result.KeyType == null)
            {
                Debug.LogError($"[EditorDictionaryConfig] 类型 {typeof(T).FullName} 未能推导 Dictionary Key 类型，不能读取 Editor Excel 缓存。");
                return false;
            }
            if (string.IsNullOrEmpty(result.SheetName))
            {
                Debug.LogError($"[EditorDictionaryConfig] 类型 {typeof(T).FullName} 缺少 ExcelSheetAttribute，不能读取 Editor Excel 缓存。");
                return false;
            }

            accessor = (IEditorDictionaryConfigAccessor<T>)Activator.CreateInstance(typeof(EditorDictionaryConfigAccessor<,>).MakeGenericType(result.KeyType, typeof(T)));
            typeData = result;
            data = typeData;
            return true;
        }

        internal static void ClearLoadedForTests()
        {
            loaded = false;
            typeData = null;
            accessor = null;
        }

        private static class EmptyConfigs
        {
            public static readonly IReadOnlyDictionary<object, T> Value = new Dictionary<object, T>();
        }
    }

    internal interface IEditorDictionaryConfigAccessor<TConfig> where TConfig : IDictionaryConfig
    {
        int Count { get; }
        IReadOnlyDictionary<object, TConfig> Configs { get; }
        void ReadFromCache();
        TConfig Get(object key);
        bool Contains(object key);
    }

    internal class EditorDictionaryConfigAccessor<TKey, TConfig> : IEditorDictionaryConfigAccessor<TConfig>
        where TKey : struct
        where TConfig : IDictionaryConfig
    {
        public int Count => ConfigDictionaryCollector<TKey, TConfig>.Configs.Count;

        public IReadOnlyDictionary<object, TConfig> Configs
        {
            get
            {
                var configs = new Dictionary<object, TConfig>();
                foreach (var kv in ConfigDictionaryCollector<TKey, TConfig>.Configs)
                {
                    configs.Add(kv.Key, kv.Value);
                }
                return configs;
            }
        }

        public void ReadFromCache()
        {
            ExportUtil.Read<TKey, TConfig>(ExcelDataManager.CachePath);
        }

        public TConfig Get(object key)
        {
            if (!TryConvertKey(key, out var typedKey))
                return default;
            return ConfigDictionaryCollector<TKey, TConfig>.Configs.TryGetValue(typedKey, out var value) ? value : default;
        }

        public bool Contains(object key)
        {
            return TryConvertKey(key, out var typedKey) && ConfigDictionaryCollector<TKey, TConfig>.Configs.ContainsKey(typedKey);
        }

        private bool TryConvertKey(object key, out TKey typedKey)
        {
            if (key is TKey value)
            {
                typedKey = value;
                return true;
            }

            Debug.LogError($"[EditorDictionaryConfig] 类型 {typeof(TConfig).FullName} 的 Key 必须是 {typeof(TKey).FullName}，当前传入 {key?.GetType().FullName ?? "null"}。");
            typedKey = default;
            return false;
        }
    }
}
