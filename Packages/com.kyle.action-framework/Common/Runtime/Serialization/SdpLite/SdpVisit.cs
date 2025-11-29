public class SdpVisit<T>
{
    public delegate void PackDelegate(SdpLite.Packer packer, uint tag, bool require, T value);
    public delegate void UnPackDelegate(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T value);
    public delegate T CreateInstanceDelegate();
    public static PackDelegate Pack;
    public static UnPackDelegate UnPack;
    public static CreateInstanceDelegate CreateInstance = () => default;
}
