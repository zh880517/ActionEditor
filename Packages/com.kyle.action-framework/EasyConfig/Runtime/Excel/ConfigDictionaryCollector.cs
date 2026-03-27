using System.Collections.Generic;
namespace EasyConfig
{
    public class ConfigDictionaryCollector<TKey, TValue>  where TKey : struct where TValue : IDictionaryConfig
    {
        public static readonly Dictionary<TKey, TValue> Configs = new Dictionary<TKey, TValue>();
    }
}