using System.Collections.Generic;

public class SdpLiteUnPacker
{
    public static void UnPackArray<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T[] value)
    {
        var unpackFunc = SdpVisit<T[]>.UnPack;
        if (unpackFunc != null)
        {
            unpackFunc(unpacker, type, ref value);
            return;
        }
        var func = SdpVisit<T>.UnPack;
        if (func != null)
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
                    func(unpacker, header.type, ref element);
                    value[i] = element;
                }
            }
            else
            {
                SdpLite.Unpacker.ThrowIncompatibleType(type);
            }
        }
        else
        {
            throw new System.Exception($"No UnPack function for type {typeof(T)}");
        }
    }

    public static void UnPackList<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref List<T> value)
    {
        var unpackFunc = SdpVisit<List<T>>.UnPack;
        if (unpackFunc != null)
        {
            unpackFunc(unpacker, type, ref value);
            return;
        }
        var func = SdpVisit<T>.UnPack;
        if (func != null)
        {
            if (type == SdpLite.DataType.Vector)
            {
                uint size = unpacker.UnpackUInt();
                if (value == null)
                {
                    value = new List<T>();
                }
                else
                {
                    value.Clear();
                }
                for (int i = 0; i < size; ++i)
                {
                    T element = default;
                    var header = unpacker.UnpackHeader();
                    func(unpacker, header.type, ref element);
                    value.Add(element);
                }
            }
            else
            {
                SdpLite.Unpacker.ThrowIncompatibleType(type);
            }
        }
        else
        {
            throw new System.Exception($"No UnPack function for type {typeof(T)}");
        }
    }

    public static void UnPackHashSet<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref HashSet<T> value)
    {
        var func = SdpVisit<T>.UnPack;
        if (func != null)
        {
            if (type == SdpLite.DataType.Vector)
            {
                uint size = unpacker.UnpackUInt();
                if (value == null)
                {
                    value = new HashSet<T>();
                }
                else
                {
                    value.Clear();
                }
                for (int i = 0; i < size; ++i)
                {
                    T element = default;
                    var header = unpacker.UnpackHeader();
                    func(unpacker, header.type, ref element);
                    value.Add(element);
                }
            }
            else
            {
                SdpLite.Unpacker.ThrowIncompatibleType(type);
            }
        }
        else
        {
            throw new System.Exception($"No UnPack function for type {typeof(T)}");
        }
    }

    public static void UnPackDictionary<TKey, TValue>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref Dictionary<TKey, TValue> value)
    {
        var keyFunc = SdpVisit<TKey>.UnPack;
        var valueFunc = SdpVisit<TValue>.UnPack;
        if (keyFunc != null && valueFunc != null)
        {
            if (type == SdpLite.DataType.Vector)
            {
                uint size = unpacker.UnpackUInt();
                if (value == null)
                {
                    value = new Dictionary<TKey, TValue>();
                }
                else
                {
                    value.Clear();
                }
                for (int i = 0; i < size; ++i)
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
            else
            {
                SdpLite.Unpacker.ThrowIncompatibleType(type);
            }
        }
        else
        {
            throw new System.Exception($"No UnPack function for type {typeof(TKey)} or {typeof(TValue)}");
        }
    }

    public static void UnPack<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T value)
    {
        var unpackFunc = SdpVisit<T>.UnPack;
        if (unpackFunc != null)
        {
            unpackFunc(unpacker, type, ref value);
            return;
        }
        throw new System.Exception($"No UnPack function for type {typeof(T)}");
    }
    public static void UnPackEnum<T>(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T value) where T : System.Enum
    {
        unpacker.Unpack(type, out int v);
        value = (T)System.Enum.ToObject(typeof(T), v);
    }


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

    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref byte[] value)
    {
        if (type == SdpLite.DataType.String)
        {
            uint size = unpacker.UnpackUInt();
            if (value == null || value.Length != size)
            {
                value = new byte[size];
            }
            unpacker.Read(value, 0, (int)size);
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref sbyte[] value)
    {
        if (type == SdpLite.DataType.String)
        {
            uint size = unpacker.UnpackUInt();
            if (value == null || value.Length != size)
            {
                value = new sbyte[size];
            }
            for(int i=0; i<size; ++i)
            {
                value[i] = (sbyte)unpacker.UnpackByte();
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }

    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref List<byte>value)
    {
        if (type == SdpLite.DataType.String)
        {
            if(value == null)
            {
                value = new List<byte>();
            }
            else
            {
                value.Clear();
            }
            uint size = unpacker.UnpackUInt();
            value.Clear();
            for (int i = 0; i < size; ++i)
            {
                value.Add(unpacker.UnpackByte());
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }

    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref List<sbyte> value)
    {
        if (type == SdpLite.DataType.String)
        {
            if (value == null)
            {
                value = new List<sbyte>();
            }
            else
            {
                value.Clear();
            }
            uint size = unpacker.UnpackUInt();
            value.Clear();
            for (int i = 0; i < size; ++i)
            {
                value.Add((sbyte)unpacker.UnpackByte());
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }

    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref bool[] value)
    {
        if (type == SdpLite.DataType.String)
        {
            uint size = unpacker.UnpackUInt();
            if (value == null || value.Length != size)
            {
                value = new bool[size];
            }
            for (int i = 0; i < size; ++i)
            {
                byte v= unpacker.UnpackByte();
                value[i] = v != 0;
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }

    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref List<bool> value)
    {
        if (type == SdpLite.DataType.String)
        {
            uint size = unpacker.UnpackUInt();
            if (value == null)
            {
                value = new List<bool>();
            }
            else
            {
                value.Clear();
            }
            for (int i = 0; i < size; ++i)
            {
                byte v = unpacker.UnpackByte();
                value.Add(v != 0);
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }

}