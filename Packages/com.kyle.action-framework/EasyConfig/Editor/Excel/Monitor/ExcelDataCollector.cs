using UnityEngine;

namespace EasyConfig.Editor
{
    public abstract class ExcelDataCollector : ScriptableObject
    {
        public string SheetName;

        internal abstract void ReadFromFile(string filePath);
    }
}
