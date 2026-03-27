using System.Collections.Generic;

namespace EasyConfig
{
    public class DictionaryConfig<TKey, T> : IDictionaryConfig where TKey : struct where T : IDictionaryConfig
    {

        public static int Count => ConfigDictionaryCollector<TKey, T>.Configs.Count;

        public static T Get(TKey key)
        {
            if (ConfigDictionaryCollector<TKey, T>.Configs.ContainsKey(key))
            {
                return ConfigDictionaryCollector<TKey, T>.Configs[key];
            }
            //UnityEngine.Debug.LogErrorFormat("缺少配置 {0} : {1}", typeof(T).Name, key);
            return default;
            //ConfigDictionaryCollector<TKey, T>.Configs.TryGetValue(key, out T value);
            //return value;
        }

        public static bool Contains(TKey key)
        {
            return ConfigDictionaryCollector<TKey, T>.Configs.ContainsKey(key);
        }

        public static IReadOnlyDictionary<TKey, T> Configs => ConfigDictionaryCollector<TKey, T>.Configs;
    }
}