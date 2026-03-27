using System;
using System.Collections;
using System.Reflection;

namespace EasyConfig.Editor
{
    public static class ConvertUtil
    {
        public static IConvert ToConvert(FieldInfo field)
        {
            if (field.FieldType.IsArray || typeof(IList).IsAssignableFrom(field.FieldType))
            {
                var sep = field.GetCustomAttribute<FieldSeparatorAttribute>();
                if (sep == null)
                {
                    throw new Exception($"List类型字段必须使用FieldSeparatorAttribute设置分隔符 {field.DeclaringType}.{field.Name}");
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(field.FieldType))
            {
                var sep = field.GetCustomAttribute<DictionarySeparatorAttribute>();
                if (sep == null)
                {
                    throw new Exception($"Dictionary类型字段必须使用DictionarySeparatorAttribute设置分隔符 {field.DeclaringType}.{field.Name}");
                }
                return new DictionaryConvert(field.FieldType, sep.Element, sep.KeyValue);
            }
            return ToConvert(field.FieldType);
        }
        public static IConvert ToConvert(Type type, char separator = char.MinValue)
        {
            if (type == typeof(bool))
                return new BoolenConvert();
            else if (type == typeof(short))
                return new ShortConvert();
            else if (type == typeof(ushort))
                return new UShortConvert();
            else if (type == typeof(int))
                return new IntConvert();
            else if (type == typeof(uint))
                return new UIntConvert();
            else if (type == typeof(long))
                return new LongConvert();
            else if (type == typeof(ulong))
                return new ULongConvert();
            else if (type == typeof(float))
                return new FloatConvert();
            else if (type == typeof(double))
                return new DoubleConvert();
            else if (type == typeof(string))
                return new StringConvert();
            else if (type == typeof(DateTime))
                return new DateTimeConvert();
            else if (type.IsArray || typeof(IList).IsAssignableFrom(type))
            {
                if (separator == char.MinValue)
                    throw new Exception("容器类型不支持直接用类型做转换");
                return new ListConvert(type, separator);
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                throw new Exception("容器类型不支持直接用类型做转换");
            }
            if (separator != char.MinValue)
            {
                return new StructConvert(type, separator);
            }
            else
            {
                var fieldSeparator = type.GetCustomAttribute<FieldSeparatorAttribute>();
                if (fieldSeparator != null)
                {
                    return new StructConvert(type, fieldSeparator.Separator);
                }
            }
            return null;
        }
    }
}