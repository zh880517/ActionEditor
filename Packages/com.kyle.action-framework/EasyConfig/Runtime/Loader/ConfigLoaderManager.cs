using System.Collections.Generic;

namespace EasyConfig
{
    public class ConfigLoaderManager
    {
        private static ConfigLoaderManager _instance;
        public static ConfigLoaderManager Instance
        {
            get
            {
                _instance ??= new ConfigLoaderManager();
                return _instance;
            }
        }

        private IDataProvider dataProvider;
        private Dictionary<string, IConfigLoader> loaders = new Dictionary<string, IConfigLoader>();

        public void RegisterLoader(IConfigLoader loader)
        {
            if (loaders.ContainsKey(loader.TypeName))
                return;
            loaders.Add(loader.TypeName, loader);
        }

        public static void OnDataModify(string type, string name, byte[] data)
        {
            if (_instance != null && _instance.loaders.TryGetValue(type, out var loader))
                loader.OnDataModify(name, data);
        }

        public void SetDataProvider(IDataProvider provider)
        {
            dataProvider = provider;
        }
        //提供给编辑器模式的设置接口，如果已经有了DataProvider则不会被覆盖
        public void TrySetDataProvider(IDataProvider provider)
        {
            dataProvider ??= provider;
        }

        public byte[] LoadData(string type, string name)
        {
            return dataProvider?.LoadData(type, name);
        }

        public static void Destroy()
        {
            if (_instance != null)
            {
                foreach (var kv in _instance.loaders)
                {
                    kv.Value.Clear();
                }
                _instance.loaders.Clear();
            }
            _instance = null;
        }

    }
}
