using System;
using System.Collections;
namespace EasyConfig.Editor
{
    public class ListConvert : IConvert
    {
        private readonly Type type;
        private readonly char separator;
        private readonly Type elementType;
        private readonly IConvert elementConvert;

        public ListConvert(Type type, char separator)
        {
            this.type = type;
            this.separator = separator;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else
            {
                elementType = type.GenericTypeArguments[0];
            }
            elementConvert = ConvertUtil.ToConvert(elementType);
        }
        public object Convert(string value)
        {
            string[] result = value.Split(separator);
            if (type.IsArray)
            {
                Array array = Array.CreateInstance(elementType, result.Length);
                for (int i=0; i<result.Length; ++i)
                {
                    array.SetValue(elementConvert.Convert(result[i]), i);
                }
                return array;
            }
            else
            {
                var val = Activator.CreateInstance(type) as IList;
                for (int i = 0; i < result.Length; ++i)
                {
                    val.Add(elementConvert.Convert(result[i]));
                }
                return val;
            }
        }
    }
}