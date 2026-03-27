namespace EasyConfig.Editor
{
    public class ColumnReader : IColumnReader
    {
        private readonly string title;
        private readonly IConvert convert;

        public ColumnReader(IConvert convert, string title)
        {
            this.convert = convert;
            this.title = title;
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

        public object Read(SheetData sheet, int rowIndex, int arrayIndex = 0)
        {
            string val = sheet.GetValue(title, rowIndex, arrayIndex);
            return convert.Convert(val);
        }
    }
}