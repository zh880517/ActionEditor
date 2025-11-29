public class SdpLiteUnPacker
{
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref bool value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref byte value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref sbyte value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref short value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref ushort value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref float value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref double value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref int value)
    {
        unpacker.Unpack(type, out value);
    }

    public static void UnPack<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T value) where T : System.Enum
    {
        unpacker.Unpack(type, out int v);
        value = (T)System.Enum.ToObject(typeof(T), v);
    }

    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref uint value)
    {
        unpacker.Unpack(type, out value);
    }

    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref long value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref ulong value)
    {
        unpacker.Unpack(type, out value);
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref string value)
    {
        value = "";
        if (type == SdpLite.DataType.String)
        {
            uint size = unpacker.UnpackUInt();
            unpacker.CheckSize(size);
            if (size > 0)
            {
                unpacker.Read(size, out value);
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }
}