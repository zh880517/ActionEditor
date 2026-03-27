namespace EasyConfig.Editor
{
    [System.Serializable]
    public class RowData
    {
        public int RowIndex;//实际的行索引，部分行信息因为注释原因被忽略
        public string[] Data;
    }
}