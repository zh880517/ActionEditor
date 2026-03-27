using System;
using System.Collections.Generic;
using System.Reflection;
namespace EasyConfig.Editor
{
    public class StructConvert : IConvert
    {
        struct FieldConvert
        {
            public FieldInfo Info;
            public IConvert Convert;
        }
        private readonly Type structType;
        private readonly char separator;
        private readonly List<FieldConvert> fieldConvert = new List<FieldConvert>();
        public StructConvert(Type type,  char separator)
        {
            structType = type;
            this.separator = separator;
            foreach (var field in type.GetFields())
            {
                if (field.IsStatic || field.IsPrivate)
                    continue;
                var convert = ConvertUtil.ToConvert(field);
                if (convert != null)
                    fieldConvert.Add(new FieldConvert { Info = field, Convert = convert });
            }
        }
        public object Convert(string value)
        {
            string[] result = value.Split(separator);
            object val = Activator.CreateInstance(structType);
            for (int i=0; i<result.Length && i<fieldConvert.Count; ++i)
            {
                var convert = fieldConvert[i];
                convert.Info.SetValue(val, convert.Convert.Convert(result[i]));
            }
            return val;
        }
    }
}