using System;

namespace EasyConfig.Editor
{
    public interface IConvert
    {
        object Convert(string value);
    }

    public class BoolenConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "0" || value == "false")
                return false;
            return true;
        }
    }

    public class ShortConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return short.Parse(value);
        }
    }
    public class UShortConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return ushort.Parse(value);
        }
    }

    public class IntConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return int.Parse(value);
        }
    }

    public class UIntConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return uint.Parse(value);
        }
    }

    public class LongConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return long.Parse(value);
        }
    }

    public class ULongConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return ulong.Parse(value);
        }
    }

    public class FloatConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return float.Parse(value);
        }
    }

    public class DoubleConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return double.Parse(value);
        }
    }

    public class DateTimeConvert : IConvert
    {
        public object Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return DateTime.Parse(value);
        }
    }


    public class StringConvert : IConvert
    {
        public object Convert(string value)
        {
            return value;
        }
    }
}