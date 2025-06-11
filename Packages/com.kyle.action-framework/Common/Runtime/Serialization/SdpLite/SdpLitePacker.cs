using System.Collections.Generic;
public class SdpLitePacker
{
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

    public static void Pack<T>(SdpLite.Packer packer, uint tag, bool require, T value) where T : System.Enum
    {
        int v = System.Convert.ToInt32(value);
        if (v != 0 || require)
            packer.Pack(tag, v);
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
    public delegate void PackFunc<T>(SdpLite.Packer packer, uint tag, bool require, T value);
    public static void Pack<T>(SdpLite.Packer packer, uint tag, bool require, T[] value, PackFunc<T> func)
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
    public static void Pack<T>(SdpLite.Packer packer, uint tag, bool require, IList<T> value, PackFunc<T> func)
    {
        uint count = (uint)value.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Vector);
            packer.Pack(count);
            foreach (var item in value)
            {
                func(packer, 0, true, item);
            }
        }
    }

    public static void Pack<T>(SdpLite.Packer packer, uint tag, bool require, ISet<T> value, PackFunc<T> func)
    {
        uint count = (uint)value.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Vector);
            packer.Pack(count);
            foreach (var item in value)
            {
                func(packer, 0, true, item);
            }
        }
    }

    public static void Pack<TKey, TValue>(SdpLite.Packer packer, uint tag, bool require, IDictionary<TKey, TValue> value, PackFunc<TKey> keyFunc, PackFunc<TValue> valueFunc)
    {
        uint count = (uint)value.Count;
        if (require || count > 0)
        {
            packer.PackHeader(tag, SdpLite.DataType.Map);
            packer.Pack(count);
            foreach (var kv in value)
            {
                keyFunc(packer, 0, true, kv.Key);
                valueFunc(packer, 0, true, kv.Value);
            }
        }
    }
}

