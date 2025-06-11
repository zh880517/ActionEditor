using System.Collections.Generic;

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
    public delegate void UnPackFunc<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T val);
    public static void UnPack<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T[] value, UnPackFunc<T> keyFunc)
    {
        if (type == SdpLite.DataType.Vector)
        {
            uint size = unpacker.UnpackUInt();
            if (value == null || value.Length != size)
            {
                value = new T[size];
            }
            for (int i = 0; i < size; ++i)
            {
                T element = default;
                var header = unpacker.UnpackHeader();
                keyFunc(unpacker, header.type, ref element);
                value[i] = element;
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }

    public static void UnPack<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref List<T> value, UnPackFunc<T> keyFunc)
    {
        if (type == SdpLite.DataType.Vector)
        {
            uint size = unpacker.UnpackUInt();
            if (value == null)
                value = new List<T>((int)size);
            else
                value.Clear();
            for (int i = 0; i < size; ++i)
            {
                T element = default;
                var header = unpacker.UnpackHeader();
                keyFunc(unpacker, header.type, ref element);
                value.Add(element);
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }
    public static void UnPack<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref HashSet<T> value, UnPackFunc<T> keyFunc)
    {
        if (type == SdpLite.DataType.Vector)
        {
            uint size = unpacker.UnpackUInt();
            if (value == null)
                value = new HashSet<T>();
            else
                value.Clear();
            for (int i = 0; i < size; ++i)
            {
                T element = default;
                var header = unpacker.UnpackHeader();
                keyFunc(unpacker, header.type, ref element);
                value.Add(element);
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }

    public static void UnPack<TDictionary, TKey, TValue>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref TDictionary value, UnPackFunc<TKey> keyFunc, UnPackFunc<TValue> valueFunc) where TDictionary : IDictionary<TKey, TValue>, new()
    {
        if (type != SdpLite.DataType.Map)
        {
            if (value == null)
                value = new TDictionary();
            else
                value.Clear();
            uint count = unpacker.UnpackUInt();
            if (count > 0)
            {
                value.Clear();
                for (int i = 0; i < count; i++)
                {
                    TKey key = default;
                    TValue val = default;
                    var keyHeader = unpacker.UnpackHeader();
                    keyFunc(unpacker, keyHeader.type, ref key);

                    var valueHeader = unpacker.UnpackHeader();
                    valueFunc(unpacker, valueHeader.type, ref val);
                    value.Add(key, val);
                }
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }
}