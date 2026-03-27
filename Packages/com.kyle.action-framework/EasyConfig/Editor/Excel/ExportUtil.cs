using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace EasyConfig.Editor
{
    public static class ExportUtil
    {
        public static void Read<T>(string path) where T : IListConfig
        {
            ConfigListCollector<T>.Configs.Clear();
            Type type = typeof(T);
            var sheetInfo = type.GetCustomAttribute<ExcelSheetAttribute>();
            if (sheetInfo == null)
                throw new Exception($"导出类型必须使用 ExcelSheetAttribute 标记需要导出的页签名字规则 : {type.Name}");

            var sheets = Read(path, sheetInfo.Name);
            IColumnReader reader = ColumnReaderUtil.ToRead<T>();
            foreach (var sheet in sheets)
            {
                for (int i = 0; i < sheet.Data.Count; ++i)
                {
                    try
                    {
                        var data = reader.Read(sheet, i);
                        ConfigListCollector<T>.Configs.Add((T)data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"导出时出错 {sheet.Name} -> 行:{sheet.Data[i].RowIndex}\n{ex.Message}\n{ex.StackTrace}");
                        break;
                    }
                }
            }
        }

        public static void Read<TKey, T>(string path) where TKey : struct where T : IDictionaryConfig
        {
            ConfigDictionaryCollector<TKey, T>.Configs.Clear();
            Type type = typeof(T);
            var sheetInfo = type.GetCustomAttribute<ExcelSheetAttribute>();
            if (sheetInfo == null)
                throw new Exception($"导出类型必须使用 ExcelSheetAttribute 标记需要导出的页签名字规则 : {type.Name}");
            var keyInfo = type.GetCustomAttribute<KeyColumnAttribute>();
            if (keyInfo == null)
                throw new Exception($"导出类型必须使用 KeyColumnAttribute 标记需要导出Key对应的列名字 : {type.Name}");
            var keyRead = new ColumnReader(ConvertUtil.ToConvert(typeof(TKey)), keyInfo.Name);
            IColumnReader reader = ColumnReaderUtil.ToRead<T>();

            var sheets = Read(path, sheetInfo.Name);
            foreach (var sheet in sheets)
            {
                for (int i = 0; i < sheet.Data.Count; ++i)
                {
                    try
                    {
                        var key = (TKey)keyRead.Read(sheet, i);
                        var value = (T)reader.Read(sheet, i);
                        ConfigDictionaryCollector<TKey, T>.Configs.Add(key, value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"导出时出错 {sheet.Name} -> 行:{sheet.Data[i].RowIndex}\n{ex.Message}\n{ex.StackTrace}");
                        break;
                    }
                }
            }
        }
        private static List<SheetData> Read(string path, string name)
        {
            var files = Directory.GetFiles(path, $"{name}.json", SearchOption.TopDirectoryOnly);
            List<SheetData> results = new List<SheetData>();
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var data = JsonUtility.FromJson<SheetData>(json);
                data.Name = Path.GetFileNameWithoutExtension(file);
                results.Add(data);
            }
            return results;
        }
    }
}