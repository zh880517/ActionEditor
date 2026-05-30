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

            var sheets = Read(path, sheetInfo.Name, sheetInfo.MultiFile);
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

            var sheets = Read(path, sheetInfo.Name, sheetInfo.MultiFile);
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
        private static List<SheetData> Read(string path, string name, bool multiFile)
        {
            var files = Directory.GetFiles(path, $"*_{name}.json", SearchOption.TopDirectoryOnly);
            if (!multiFile && files.Length > 1)
                Debug.LogWarning($"页签 \"{name}\" 存在多个来源文件，但未启用分表（MultiFile=false），只读取第一个：{files[0]}");
            List<SheetData> results = new List<SheetData>();
            int count = multiFile ? files.Length : Mathf.Min(files.Length, 1);
            for (int i = 0; i < count; i++)
            {
                var json = File.ReadAllText(files[i]);
                var data = JsonUtility.FromJson<SheetData>(json);
                data.Name = Path.GetFileNameWithoutExtension(files[i]);
                results.Add(data);
            }
            return results;
        }
    }
}