using System.Collections.Generic;

[System.Serializable]
public class ExcelReadCache
{
    [System.Serializable]
    public class ExcelInfo
    {
        public string Name;
        public List<string> Sheets = new List<string>();
        public string LastReadTime;
    }

    public List<ExcelInfo> Excels = new List<ExcelInfo>();
}
