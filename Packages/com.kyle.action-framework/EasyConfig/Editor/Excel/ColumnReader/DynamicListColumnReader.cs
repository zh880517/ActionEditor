using System;
using System.Collections;
using System.Collections.Generic;
namespace EasyConfig.Editor
{
    public class DynamicListColumnReader : IColumnReader
    {
        private readonly Type type;
        private readonly Type elementType;
        private readonly string title;
        private readonly IConvert elementConvert;
        public DynamicListColumnReader(Type type, string title, char separator = char.MinValue)
        {
            this.type = type;
            this.title = title;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else
            {
                elementType = type.GenericTypeArguments[0];
            }
            elementConvert = ConvertUtil.ToConvert(elementType, separator);
        }
        public int GetArrayCount(SheetData sheet)
        {
            var lastIndex = sheet.Titiles.FindLastIndex(it => it.Name == title);
            if (lastIndex >= 0)
            {
                return sheet.Titiles[lastIndex].ArrayIndex + 1;
            }
            return 0;
        }

        public object Read(SheetData sheet, int rowIndex, int arrayIndex)
        {
            int count = GetArrayCount(sheet);
            if (type.IsArray)
            {
                List<string> datas = new List<string>();
                for (int i = 0; i < count; ++i)
                {
                    string data = sheet.GetValue(title, rowIndex, i);
                    if (!string.IsNullOrEmpty(data))
                        datas.Add(data);
                }
                Array array = Array.CreateInstance(elementType, datas.Count);
                for (int i=0; i<datas.Count; ++i)
                {
                    array.SetValue(elementConvert.Convert(datas[i]), i);
                }
                return array;
            }
            else
            {
                var val = Activator.CreateInstance(type) as IList;
                for (int i = 0; i < count; ++i)
                {
                    string data = sheet.GetValue(title, rowIndex, i);
                    if (!string.IsNullOrEmpty(data))
                        val.Add(elementConvert.Convert(data));
                }
                return val;
            }
        }
    }
}