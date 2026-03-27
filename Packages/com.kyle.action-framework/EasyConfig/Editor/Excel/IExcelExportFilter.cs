public interface IExcelExportFilter
{
    bool CheckSheetName(string name);
    bool CheckExcelName(string name);
}


public class DefaultExcelExportFilter : IExcelExportFilter
{
    public bool CheckExcelName(string name)
    {
        return true;
    }

    public bool CheckSheetName(string name)
    {
        return true;
    }
}