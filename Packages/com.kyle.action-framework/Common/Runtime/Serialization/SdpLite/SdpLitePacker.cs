using System.Collections.Generic;
public class SdpLitePacker
{
    public static void PackArray<T>(SdpLite.Packer packer, uint tag, bool require, T[] value)
    {
        var packFunc = SdpVisit<T[]>.Pack;
        if (packFunc != null)
        {
            packFunc(packer, tag, require, value);
            return;
        }
        var func = SdpVisit<T>.Pack;
        if (func != null)
        {
            int count = value != null ? value.Length : 0;
            if (require || count > 0)
            {
                packer.PackHeader(tag, SdpLite.DataType.Vector);
                packer.Pack((uint)count);
                if (count > 0)
                {
                    foreach (var item in value)
                    {
                        func(packer, 0, true, item);
                    }
                }
            }
        }
        else
        {
            throw new System.Exception($"No Pack function for type {typeof(T)}");
        }
    }

    public static void PackList<T>(SdpLite.Packer packer, uint tag, bool require, List<T> value)
    {
        var packFunc = SdpVisit<List<T>>.Pack;
        if (packFunc != null)
        {
            packFunc(packer, tag, require, value);
            return;
        }
        var func = SdpVisit<T>.Pack;
        if (func != null)
        {
            int count = value != null ? value.Count : 0;
            if (require || count > 0)
            {
                packer.PackHeader(tag, SdpLite.DataType.Vector);
                packer.Pack((uint)count);
                if (count > 0)
                {
                    foreach (var item in value)
                    {
                        func(packer, 0, true, item);
                    }
                }
            }
        }
        else
        {
            throw new System.Exception($"No Pack function for type {typeof(T)}");
        }
    }

    public static void PackHashSet<T>(SdpLite.Packer packer, uint tag, bool require, HashSet<T> value)
    {
        var packFunc = SdpVisit<T>.Pack;
        if (packFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(T)}");
        uint count = (uint)value.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Vector);
            packer.Pack(count);
            foreach (var item in value)
            {
                packFunc(packer, 0, true, item);
            }
        }
    }

    public static void PackDictionary<TKey, TValue>(SdpLite.Packer packer, uint tag, bool require, Dictionary<TKey, TValue> dict)
    {
        var packKeyFunc = SdpVisit<TKey>.Pack;
        var packValueFunc = SdpVisit<TValue>.Pack;
        if (packKeyFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(TKey)}");
        if (packValueFunc == null)
            throw new System.Exception($"No Pack function for type {typeof(TValue)}");
        uint count = (uint)dict.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Map);
            packer.Pack(count);
            foreach (var kvp in dict)
            {
                packKeyFunc(packer, 0, true, kvp.Key);
                packValueFunc(packer, 0, true, kvp.Value);
            }
        }
    }
    
    public static void Pack<T>(SdpLite.Packer packer, uint tag, bool require, T value)
    {
        var packFunc = SdpVisit<T>.Pack;
        if (packFunc != null)
        {
            packFunc(packer, tag, require, value);
            return;
        }
        throw new System.Exception($"No Pack function for type {typeof(T)}");
    }

    public static void PackEnum<T>(SdpLite.Packer packer, uint tag, bool require, T value) where T : System.Enum
    {
        int v = System.Convert.ToInt32(value);
        if (v != 0 || require)
            packer.Pack(tag, v);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, bool value)
    {
        if (value || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, byte value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, sbyte value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, short value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, ushort value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, float value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, double value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, int value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, uint value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, long value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, ulong value)
    {
        if (value != 0 || require)
            packer.Pack(tag, value);
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, string value)
    {
        if (!string.IsNullOrEmpty(value) || require)
        {
            uint bytesLen = (uint)System.Text.Encoding.UTF8.GetByteCount(value);
            packer.PackHeader(tag, SdpLite.DataType.String);
            packer.Pack(bytesLen);
            packer.Pack(value, bytesLen);
        }
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, byte[] value)
    {
        uint count = (uint)(value != null ? value.Length : 0);
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.String);
            packer.Pack(count);
            if (count > 0)
            {
                packer.Pack(value, 0, (int)count);
            }
        }
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, List<byte> value)
    {
        uint count = (uint)(value != null ? value.Count : 0);
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.String);
            packer.Pack(count);
            for (int i = 0; i < count; i++)
            {
                packer.Pack(value[i]);
            }
        }
    }

    public static void Pack(SdpLite.Packer packer, uint tag, bool require, sbyte[] value)
    {
        uint count = (uint)(value != null ? value.Length : 0);
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.String);
            packer.Pack(count);
            for (int i = 0; i < count; i++)
            {
                packer.Pack((byte)value[i]);
            }
        }
    }
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, List<sbyte> value)
    {
        uint count = (uint)(value != null ? value.Count : 0);
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.String);
            packer.Pack(count);
            for (int i = 0; i < count; i++)
            {
                packer.Pack((byte)value[i]);
            }
        }
    }
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, bool[] value)
    {
        uint count = (uint)(value != null ? value.Length : 0);
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.String);
            packer.Pack(count);
            for (int i = 0; i < count; i++)
            {
                packer.Pack(value[i] ? (byte)1 : (byte)0);
            }
        }
    }
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, List<bool> value)
    {
        uint count = (uint)(value != null ? value.Count : 0);
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.String);
            packer.Pack(count);
            for (int i = 0; i < count; i++)
            {
                packer.Pack(value[i] ? (byte)1 : (byte)0);
            }
        }
    }
}

