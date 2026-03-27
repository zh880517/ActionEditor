namespace EasyConfig.Editor
{
    public interface IColumnReader
    {
        object Read(SheetData sheet, int rowIndex, int arrayIndex = 0);
        int GetArrayCount(SheetData sheet);
    }
}