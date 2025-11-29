using System.Collections.Generic;

public class SdpVisit<T>
{
    public delegate void PackDelegate(SdpLite.Packer packer, uint tag, bool require, T value);
    public delegate void UnPackDelegate(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T value);

    public static PackDelegate Pack;
    public static UnPackDelegate UnPack;
}

public class SdpArrayVisit<T>
{
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, T[] value)
    {
        var packFunc = SdpVisit<T>.Pack;
        if (packFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(T)}");

        int count = value != null ? value.Length : 0;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Vector);
            packer.Pack((uint)count);
            if (count > 0)
            {
                foreach (var item in value)
                {
                    packFunc(packer, 0, true, item);
                }
            }
        }
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref T[] value)
    {
        var unPackFunc = SdpVisit<T>.UnPack;
        if (unPackFunc == null)
            throw new System.Exception($"No UnPack function for type {typeof(T)}");
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
                unPackFunc(unpacker, header.type, ref element);
                value[i] = element;
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }
}

public class SdpListVisit<T>
{
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, List<T> list)
    {
        var packFunc = SdpVisit<T>.Pack;
        if (packFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(T)}");

        uint count = (uint)list.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Vector);
            packer.Pack(count);
            foreach (var item in list)
            {
                packFunc(packer, 0, true, item);
            }
        }
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref List<T> value)
    {
        var unPackFunc = SdpVisit<T>.UnPack;
        if (unPackFunc == null)
            throw new System.Exception($"No UnPack function for type {typeof(T)}");

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
                unPackFunc(unpacker, header.type, ref element);
                value.Add(element);
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }
}

public class SdpHashSetVisit<T>
{
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, HashSet<T> set)
    {
        var packFunc = SdpVisit<T>.Pack;
        if (packFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(T)}");
        uint count = (uint)set.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Vector);
            packer.Pack(count);
            foreach (var item in set)
            {
                packFunc(packer, 0, true, item);
            }
        }
    }
    public void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref HashSet<T> value)
    {
        var unPackFunc = SdpVisit<T>.UnPack;
        if (unPackFunc == null)
            throw new System.Exception($"No UnPack function for type {typeof(T)}");
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
                unPackFunc(unpacker, header.type, ref element);
                value.Add(element);
            }
        }
        else
        {
            SdpLite.Unpacker.ThrowIncompatibleType(type);
        }
    }
}

public class SdpDictionaryVisit<TKey, TValue>
{
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, Dictionary<TKey, TValue> dict)
    {
        var keyFunc = SdpVisit<TKey>.Pack;
        if (keyFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(TKey)}");
        var valueFunc = SdpVisit<TValue>.Pack;
        if (valueFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(TValue)}");
        uint count = (uint)dict.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Map);
            packer.Pack(count);
            foreach (var kv in dict)
            {
                keyFunc(packer, 0, true, kv.Key);
                valueFunc(packer, 0, true, kv.Value);
            }
        }
    }
    public static void UnPack(SdpLite.Unpacker unpacker, SdpLite.DataType type, ref Dictionary<TKey, TValue> value)
    {
        var keyFunc = SdpVisit<TKey>.UnPack;
        if (keyFunc == null)
            throw new System.Exception($"No UnPack function for type {typeof(TKey)}");
        var valueFunc = SdpVisit<TValue>.UnPack;
        if (valueFunc == null)
            throw new System.Exception($"No UnPack function for type {typeof(TValue)}");
        if (type != SdpLite.DataType.Map)
        {
            if (value == null)
                value = new Dictionary<TKey, TValue>();
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