using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyConfig.Editor
{
    public class ExcelDataManager : ScriptableSingleton<ExcelDataManager>
    {
        [SerializeField]
        private List<ExcelDataCollector> collectors = new List<ExcelDataCollector>();
        [SerializeField]
        private List<string> modifySheets = new List<string>();

        public static string ExcelPath;
        public IExcelExportFilter ExportFilter;
        private static string CachePath => "/Library/ExcelCache/";
        private FileSystemWatcher watcher;
        private bool waitRefresh;
        public void OnDataCollectorCreate(ExcelDataCollector collector)
        {
            if (!collectors.Contains(collector))
            {
                collectors.Add(collector);
                string path = $"{CachePath}/{collector.SheetName}.json";
                if (File.Exists(path))
                {
                    collector.ReadFromFile(path);
                }
            }
        }

        public T FindCollectors<T>() where T : ExcelDataCollector
        {
            foreach (var collector in collectors)
            {
                if (collector is T)
                    return collector as T;
            }
            return null;
        }

        public void OnExceleFileChange()
        {
            waitRefresh = false;
            if (string.IsNullOrEmpty(ExcelPath))
                return;

            var expoter = new ExcelToCache(ExcelPath, CachePath, ExportFilter);
            expoter.Export((sheetName) => 
            {
                if (!modifySheets.Contains(sheetName))
                    modifySheets.Add(sheetName);
            });
        }
        public void UpdateByModify()
        {
            if (modifySheets.Count == 0)
                return;
            foreach (var collector in collectors)
            {
                if (modifySheets.Contains(collector.SheetName))
                    collector.ReadFromFile($"{CachePath}/{collector.SheetName}.json");
            }
            modifySheets.Clear();
        }

        private void OnEnable()
        {
            watcher = new FileSystemWatcher(ExcelPath, "*.xlsx");
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += OnFileChange;
            watcher.EnableRaisingEvents = true;
        }

        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.Name.Contains('~'))
                return;
            if (!waitRefresh)
            {
                waitRefresh = true;
                EditorApplication.delayCall += OnExceleFileChange;
            }
        }

        private void OnDisable()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        }
    }
}
