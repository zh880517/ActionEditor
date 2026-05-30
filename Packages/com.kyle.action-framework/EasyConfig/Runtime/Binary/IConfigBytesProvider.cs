namespace EasyConfig
{
    public interface IConfigBytesProvider
    {
        byte[] LoadBytes(string group, string fileName);
    }
}
