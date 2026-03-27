using System.Collections.Generic;

namespace EasyConfig
{
    public abstract class ConfigLoader<TData> : IConfigLoader
    {
        private readonly Dictionary<string, TData> datas = new Dictionary<string, TData>();
        public abstract string TypeName { get; }

        public ConfigLoader()
        {
            ConfigLoaderManager.Instance.RegisterLoader(this);
        }

        public void Clear()
        {
            datas.Clear();
        }

        public void OnDataModify(string name, byte[] data)
        {
            var tData = ToData(data);
            if (tData != null)
            {
                datas[name] = tData;
            }
        }

        protected TData GetData(string name)
        {
            if (datas.TryGetValue(name, out var data))
            {
                return data;
            }
            var bytes = ConfigLoaderManager.Instance.LoadData(TypeName, name);
            if (bytes != null)
            {
                var tData = ToData(bytes);
                datas[name] = tData;
                return tData;
            }
            datas.Add(name, default);
            return default;
        }

        protected abstract TData ToData(byte[] data);

    }
}
