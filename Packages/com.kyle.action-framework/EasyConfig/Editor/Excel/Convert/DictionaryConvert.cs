using System;
using System.Collections;
namespace EasyConfig.Editor
{
    public class DictionaryConvert : IConvert
    {
        private readonly Type type;
        private readonly Type keyType;
        private readonly Type valueType;
        private readonly IConvert keyConvert;
        private readonly IConvert valueConvert;
        private readonly char elementSpearator;
        private readonly char kvSeparator;

        public DictionaryConvert(Type type, char elementSpearator, char kvSeparator)
        {
            this.type = type;
            this.elementSpearator = elementSpearator;
            this.kvSeparator = kvSeparator;
            var genericTypeArguments = type.GenericTypeArguments;
            keyType = genericTypeArguments[0];
            valueType = genericTypeArguments[1];
            keyConvert = ConvertUtil.ToConvert(keyType);
            valueConvert = ConvertUtil.ToConvert(valueType);
        }
        public object Convert(string value)
        {
            var val = Activator.CreateInstance(type) as IDictionary;
            string[] result = value.Split(elementSpearator);
            for (int i=0; i<result.Length; ++i)
            {
                var kv = result[i].Split(kvSeparator);

                object key = keyConvert.Convert(kv[0]);
                if (kv.Length > 1)
                {
                    val.Add(key, valueConvert.Convert(kv[1]));
                }
                else
                {
                    val.Add(key, valueConvert.Convert(string.Empty));
                }
            }
            return val;
        }
    }
}