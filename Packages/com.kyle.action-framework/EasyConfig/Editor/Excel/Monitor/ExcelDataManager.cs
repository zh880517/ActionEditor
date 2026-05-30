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
        internal static string CachePath => Path.Combine(Directory.GetCurrentDirectory(), "Library", "ExcelCache");
        private FileSystemWatcher watcher;
        private bool waitRefresh;
        public void OnDataCollectorCreate(ExcelDataCollector collector)
        {
            if (!collectors.Contains(collector))
            {
                collectors.Add(collector);
                var files = GetSheetFiles(collector);
                if (files.Length > 0)
                    collector.ReadFromFiles(files);
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
                {
                    var files = GetSheetFiles(collector);
                    if (files.Length > 0)
                        collector.ReadFromFiles(files);
                }
            }
            modifySheets.Clear();
        }

        private string[] GetSheetFiles(ExcelDataCollector collector)
        {
            var all = Directory.GetFiles(CachePath, $"*_{collector.SheetName}.json", SearchOption.TopDirectoryOnly);
            if (!collector.MultiFile && all.Length > 1)
            {
                Debug.LogWarning($"页签 \"{collector.SheetName}\" 存在多个来源文件，但未启用分表（MultiFile=false），只读取第一个：{all[0]}");
                return new[] { all[0] };
            }
            return all;
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(ExcelPath)) return;
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
