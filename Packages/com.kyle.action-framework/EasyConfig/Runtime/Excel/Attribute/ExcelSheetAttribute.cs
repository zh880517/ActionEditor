using System;
namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExcelSheetAttribute : Attribute
    {
        public string Name { get; private set; }
        public bool MultiFile { get; private set; }

        public ExcelSheetAttribute(string name, bool multiFile = false)
        {
            Name = name;
            MultiFile = multiFile;
        }
    }
}