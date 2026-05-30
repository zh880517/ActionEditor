using UnityEngine;

namespace EasyConfig.Editor
{
    public abstract class ExcelDataCollector : ScriptableObject
    {
        public string SheetName;
        public bool MultiFile;

        internal abstract void ReadFromFile(string filePath);

        internal virtual void ClearData() { }

        internal void ReadFromFiles(string[] filePaths)
        {
            ClearData();
            foreach (var path in filePaths)
                ReadFromFile(path);
        }
    }
}
