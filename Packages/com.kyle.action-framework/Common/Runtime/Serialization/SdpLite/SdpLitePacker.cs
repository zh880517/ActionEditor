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
    public static void PackEnum<T>(SdpLite.Packer packer, uint tag, bool require, T value) where T : System.Enum
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

    public static void PackBytes(SdpLite.Packer packer, uint tag, bool require, byte[] value)
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
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, bool[] value)
    {
        uint count = (uint)(value != null ? value.Length : 0);
        if (require || count > 0)
        {
            if(count > 0)
            {
                packer.Pack(tag, value);
            }
            else
            {
                packer.PackHeader(tag, SdpLite.DataType.String);
                packer.Pack(0);
            }
        }
    }
    public static void Pack(SdpLite.Packer packer, uint tag, bool require, List<bool> value)
    {
        uint count = (uint)(value != null ? value.Count : 0);
        if (require || count > 0)
        {
            if (count > 0)
            {
                packer.Pack(tag, value);
            }
            else
            {
                packer.PackHeader(tag, SdpLite.DataType.String);
                packer.Pack(0);
            }
        }
    }

}

