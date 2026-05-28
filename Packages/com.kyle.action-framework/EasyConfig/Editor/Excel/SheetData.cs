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
            int dataIndex = Titiles.IndexOf(title);
            if (row.Data == null)
                return null;
            if (row.Data.Length == Titiles.Count && dataIndex >= 0 && dataIndex < row.Data.Length)
                return row.Data[dataIndex];
            if (title.Index >= 0 && title.Index < row.Data.Length)
                return row.Data[title.Index];
            return null;
        }
    }
}
