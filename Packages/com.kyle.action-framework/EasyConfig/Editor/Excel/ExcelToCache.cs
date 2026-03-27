using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace EasyConfig.Editor
{
    public class ExcelToCache
    {
        private const string CacheFileName = "~cachInfo.json";
        private readonly string ExcelPath;
        private readonly string CachePath;
        private readonly IExcelExportFilter ExportFilter = new DefaultExcelExportFilter();

        private readonly ExcelReadCache CacheData = new ExcelReadCache();

        public ExcelToCache(string excelPath, string cachePath, IExcelExportFilter filter)
        {
            ExcelPath = excelPath;
            CachePath = cachePath;
            if (filter != null)
                ExportFilter = filter;

            string cacheFilePath = Path.Combine(CachePath, CacheFileName);
            if (File.Exists(cacheFilePath))
            {
                string json = File.ReadAllText(cacheFilePath);
                JsonUtility.FromJsonOverwrite(json, CacheData);
            }
        }


        public void Export(Action<string> modifyCallback)
        {
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);
            var files = Directory.GetFiles(ExcelPath, "*.xlsx");
            //当前读取过的页签
            HashSet<string> hasExportSheets = new HashSet<string>();
            //当前有效的Excel表格
            HashSet<string> excels = new HashSet<string>();
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName[0] == '~' || !ExportFilter.CheckExcelName(fileName))
                    continue;
                excels.Add(fileName);

                var cacheInfo = CacheData.Excels.Find(it => it.Name == fileName);
                string lastWriteTime = File.GetLastWriteTimeUtc(file).ToString();
                if (cacheInfo != null && cacheInfo.LastReadTime == lastWriteTime)
                    continue;

                List<string> sheets = new List<string>();
                int step = 0;

                using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //注意如果项目里面没有ICSharpCode.SharpZipLib，则需要去掉 using，否则会因为资源释放找不到对应的接口而崩溃

                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        do 
                        {
                            if (!ExportFilter.CheckSheetName(reader.Name))
                                continue;
                            sheets.Add(reader.Name);
                            EditorUtility.DisplayProgressBar("读取Excel", file, (float)++step / files.Length);

                            SheetData sheetData = ReadSheet(reader);
                            string exportFilePath = Path.Combine(CachePath, $"{reader.Name}.json");
                            string writeJson = JsonUtility.ToJson(sheetData, true);
                            if (File.Exists(exportFilePath))
                            {
                                string json = File.ReadAllText(exportFilePath);
                                if (json == writeJson)
                                    continue;
                            }
                            File.WriteAllText(exportFilePath, writeJson);
                            modifyCallback?.Invoke(reader.Name);
                        } while (reader.NextResult());
                    }
                }
                EditorUtility.ClearProgressBar();
                if (cacheInfo == null)
                {
                    cacheInfo = new ExcelReadCache.ExcelInfo() { LastReadTime = lastWriteTime, Name = fileName, Sheets  = sheets};
                    CacheData.Excels.Add(cacheInfo);
                }
                else
                {
                    //清理不存在的页签
                    foreach (var sheet in cacheInfo.Sheets)
                    {
                        if (!sheets.Contains(sheet) && !hasExportSheets.Contains(sheet))
                        {
                            string exportFilePath = Path.Combine(CachePath, sheet + ".json");
                            if (File.Exists(exportFilePath))
                                File.Delete(exportFilePath);
                        }
                    }
                    cacheInfo.Sheets = sheets;
                    cacheInfo.LastReadTime = lastWriteTime;
                }
            }
            //清理已经不存在的Excel表格的导出内容
            for (int i=0; i<CacheData.Excels.Count; ++i)
            {
                var excel = CacheData.Excels[i];
                if (!excels.Contains(excel.Name))
                {
                    --i;
                    CacheData.Excels.RemoveAt(i);
                    foreach (var sheet in excel.Sheets)
                    {
                        if (!hasExportSheets.Contains(sheet))
                        {
                            string exportFilePath = Path.Combine(CachePath, sheet + ".json");
                            if (File.Exists(exportFilePath))
                                File.Delete(exportFilePath);
                        }
                    }
                }
            }

            string cacheFilePath = Path.Combine(CachePath, CacheFileName);
            File.WriteAllText(cacheFilePath, JsonUtility.ToJson(CacheData, true));
        }

        private SheetData ReadSheet(IExcelDataReader reader)
        {
            SheetData sheetData = new SheetData();
            int rowIndex = 0;
            int columnCount = reader.FieldCount;
            while (reader.Read())
            {
                rowIndex++;
                bool isNullRow = true;
                var rowStart = reader.GetValue(0);
                //#开头的行是被注释掉的，不读
                if (rowStart != null && rowStart.ToString().StartsWith("#"))
                    continue;
                string[] row = new string[columnCount];
                for (int i = 0; i < columnCount; ++i)
                {
                    var val = reader.GetValue(i);
                    string strVal = "";
                    if (val != null)
                    {
                        isNullRow = false;
                        if (val is DateTime time)
                        {
                            var fmt = reader.GetNumberFormatString(i);
                            strVal = time.ToString(fmt.Replace('h', 'H'));
                        }
                        else
                        {
                            strVal = val.ToString();
                        }
                    }
                    row[i] = strVal;
                }
                //跳过空行
                if (isNullRow)
                    continue;
                if (sheetData.Titiles.Count == 0)
                {
                    //读取标题
                    for (int i=0; i<columnCount; ++i)
                    {
                        if (!string.IsNullOrWhiteSpace(row[i]))
                        {
                            int arrayIndex = 0;
                            string name = row[i];
                            var lastIndex = sheetData.Titiles.FindLastIndex(it => it.Name == name);
                            if (lastIndex >= 0)
                            {
                                arrayIndex = sheetData.Titiles[lastIndex].ArrayIndex + 1;
                            }
                            sheetData.Titiles.Add(new ColumnTitle { Name = name, Index = i , ArrayIndex = arrayIndex});
                        }
                    }
                }
                else
                {
                    RowData rowData = new RowData() { RowIndex = rowIndex, Data = new string[sheetData.Titiles.Count] };
                    for (int i=0; i< sheetData.Titiles.Count; ++i)
                    {
                        rowData.Data[i] = row[sheetData.Titiles[i].Index];
                    }
                    sheetData.Data.Add(rowData);
                }
            }
            return sheetData;
        }
    }

}