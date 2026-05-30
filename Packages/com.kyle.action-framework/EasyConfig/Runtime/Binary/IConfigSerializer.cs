namespace EasyConfig
{
    public interface IConfigSerializer
    {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] bytes);
    }
}
