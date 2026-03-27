using System;
using System.Collections;
namespace EasyConfig.Editor
{
    public class ListColumnReader : IColumnReader
    {
        private readonly Type type;
        private readonly Type elementType;
        private readonly IColumnReader elementReader;

        public ListColumnReader(Type type, string title)
        {
            this.type = type;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else
            {
                elementType = type.GenericTypeArguments[0];
            }
            if (title.EndsWith('.') && !ColumnReaderUtil.IsBaseType(elementType))
            {
                elementReader = new StructColumnReader(elementType, title);
            }
            else
            {
                elementReader = new ColumnReader(ConvertUtil.ToConvert(elementType), title);
            }
        }

        public int GetArrayCount(SheetData sheet)
        {
            return elementReader.GetArrayCount(sheet);
        }

        public object Read(SheetData sheet, int rowIndex, int arrayIndex)
        {
            int count = GetArrayCount(sheet);
            if (type.IsArray)
            {
                Array array = Array.CreateInstance(elementType, count);
                for (int i=0; i<count; ++i)
                {
                    array.SetValue(elementReader.Read(sheet, rowIndex, i), i);
                }
                return array;
            }
            else
            {
                var val = Activator.CreateInstance(type) as IList;
                for (int i = 0; i < count; ++i)
                {
                    val.Add(elementReader.Read(sheet, rowIndex, i));
                }
                return val;
            }
        }
    }
}