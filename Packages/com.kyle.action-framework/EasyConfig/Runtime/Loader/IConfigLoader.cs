namespace EasyConfig
{
    public interface IConfigLoader
    {
        public abstract string TypeName { get; }
        public abstract void OnDataModify(string name, byte[] data);
        public abstract void Clear();
    }
}
