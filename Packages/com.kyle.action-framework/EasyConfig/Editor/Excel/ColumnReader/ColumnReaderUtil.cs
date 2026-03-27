using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace EasyConfig.Editor
{
    public static class ColumnReaderUtil
    {
        private static HashSet<Type> baseType = new HashSet<Type>
        {
             typeof(bool),
             typeof(short),
             typeof(ushort),
             typeof(int),
             typeof(uint),
             typeof(long),
             typeof(ulong),
             typeof(float),
             typeof(double),
             typeof(string),
             typeof(DateTime),
        };

        public static bool IsBaseType(Type type)
        {
            return baseType.Contains(type);
        }
        

        public static IColumnReader ToRead<T>()
        {
            var type = typeof(T);
            if (baseType.Contains(type) || type.IsArray || typeof(IList).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type))
            {
                throw new Exception($"该类型不支持直接导出 : {type.Name}");
            }
            return new StructColumnReader(type, null);
        }
    }
}
