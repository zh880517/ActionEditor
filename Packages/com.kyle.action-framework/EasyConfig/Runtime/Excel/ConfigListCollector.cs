using System.Collections.Generic;
namespace EasyConfig
{
    public class ConfigListCollector<T> where T : IListConfig
    {
        public static List<T> Configs = new List<T>();
    }
}