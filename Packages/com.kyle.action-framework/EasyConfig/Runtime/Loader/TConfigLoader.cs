namespace EasyConfig
{
    public abstract class TConfigLoader<TLoader, TData> : ConfigLoader<TData> where TLoader : TConfigLoader<TLoader, TData>, new()
    {
        private static TLoader _instance;
        protected static TLoader Instance
        {
            get
            {
                _instance ??= new TLoader();
                return _instance;
            }
        }

        public static void Destroy()
        {
            _instance = null;
        }

        public static TData Get(string name)
        {
            return Instance.GetData(name);
        }
    }
}
