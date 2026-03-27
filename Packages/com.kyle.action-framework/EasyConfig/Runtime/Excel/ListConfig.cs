using System;
using System.Collections.Generic;

namespace EasyConfig
{
    public class ListConfig<T> : IListConfig where T : IListConfig
    {
        public static int Count => ConfigListCollector<T>.Configs.Count;
        public static IReadOnlyList<T> Configs => ConfigListCollector<T>.Configs;
        public static T Get(int index)
        {
            return ConfigListCollector<T>.Configs[index];
        }
        public static int FindIndex(Predicate<T> match)
        {
            return ConfigListCollector<T>.Configs.FindIndex(match);
        }
        public static int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return ConfigListCollector<T>.Configs.FindIndex(startIndex, count, match);
        }

        public static T Find(Predicate<T> match)
        {
            return ConfigListCollector<T>.Configs.Find(match);
        }
        public static bool Exists(Predicate<T> match)
        {
            return ConfigListCollector<T>.Configs.Exists(match);
        }
    }
}