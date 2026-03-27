using System.Collections.Generic;
namespace EasyConfig.Editor
{
    [System.Serializable]
    public class ColumnTitle
    {
        public string Name;
        public int Index;
        public int ArrayIndex;
    }

    [System.Serializable]
    public class SheetData
    {
        public List<ColumnTitle> Titiles = new List<ColumnTitle>();
        public List<RowData> Data = new List<RowData>();
        public string Name { get; set; }

        public string GetValue(string name, int rowIndex, int arrayIndex = 0)
        {
            if (rowIndex < 0 || rowIndex >= Data.Count)
                return null;
            var title = Titiles.Find(it => it.Name == name && it.ArrayIndex == arrayIndex);
            if (title == null)
                return null;
            var row = Data[rowIndex];
            return row.Data[title.Index];
        }
    }
}