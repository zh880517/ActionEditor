using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace EasyConfig.Editor
{
    public class StructColumnReader : IColumnReader
    {
        struct Field
        {
            public FieldInfo Info;
            public IColumnReader Reader;
        }
        private readonly Type type;
        private readonly List<Field> fields = new List<Field>();

        public StructColumnReader(Type type, string title)
        {
            this.type = type;
            foreach (var field in type.GetFields())
            {
                if (field.IsStatic || field.IsPrivate)
                    continue;
                var reader = ToRead(field, title);
                if (reader != null)
                {
                    fields.Add(new Field { Info = field, Reader = reader });
                }
            }
        }

        private IColumnReader ToRead(FieldInfo field, string prefixName)
        {
            if (field.FieldType.IsArray || typeof(IList).IsAssignableFrom(field.FieldType))
            {
                var dynamicList = field.GetCustomAttribute<DynimaicListAttribute>();
                if (dynamicList != null)
                {
                    string title = dynamicList.Name;
                    if (!string.IsNullOrEmpty(prefixName))
                        title = prefixName + title;
                    return new DynamicListColumnReader(field.FieldType, title, dynamicList.Separator);
                }
            }
            string indexName = field.Name;
            var columnIndex = field.GetCustomAttribute<ColumnNameAttribute>();
            if (columnIndex != null)
                indexName = columnIndex.Name;
            if (!string.IsNullOrEmpty(prefixName))
                indexName = $"{prefixName}{indexName}";
            if (field.FieldType.IsArray || typeof(IList).IsAssignableFrom(field.FieldType))
            {
                return new ListColumnReader(field.FieldType, indexName);
            }
            if (ColumnReaderUtil.IsBaseType(field.FieldType))
            {
                return new ColumnReader(ConvertUtil.ToConvert(field.FieldType), indexName);
            }
            if (indexName.EndsWith('.'))
            {
                return new StructColumnReader(field.FieldType, indexName);
            }
            return new ColumnReader(ConvertUtil.ToConvert(field.FieldType), indexName);
        }
        public object Read(SheetData sheet, int rowIndex, int arrayIndex)
        {
            var val = Activator.CreateInstance(type);
            foreach (var field in fields)
            {
                field.Info.SetValue(val, field.Reader.Read(sheet, rowIndex , arrayIndex));
            }
            return val;
        }

        public int GetArrayCount(SheetData sheet)
        {
            int count = 0;
            foreach (var field in fields)
            {
                var arrayCount = field.Reader.GetArrayCount(sheet);
                if (arrayCount > count)
                    count = arrayCount;
            }
            return count;
        }
    }
}