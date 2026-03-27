using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EasyConfig.Editor
{
    public class DictionaryDataCollectorT<TCollector, TItem, TKey> : ExcelDataCollector where TItem:IDictionaryConfig where TCollector : ExcelDataCollector
    {
        [Serializable]
        public struct ItemeData
        {
            public TKey Key;
            public TItem Data;
        }

        public string KeyName;
        [SerializeField]
        private List<ItemeData> items = new List<ItemeData>();
        
        private static ExcelDataCollector instance;

        public List<KeyValuePair<TKey, string>> searchList;

        public static IReadOnlyList<KeyValuePair<TKey, string>> SearchList
        {
            get
            {
                var collector = GetInstance();
                if (collector.searchList == null || collector.searchList.Count == 0)
                {
                    collector.searchList = collector.items.Select(it => new KeyValuePair<TKey, string>(it.Key, $"{it.Key}_{ collector.GetShowName(it.Data)}"))
                        .Prepend(new KeyValuePair<TKey, string>(default, "无"))
                        .OrderBy(it => it.Key)
                        .ToList();
                }
                return collector.searchList;
            }
        }

        public static TItem Find(TKey key)
        {
            var collector = GetInstance();
            for (int i=0; i< collector.items.Count; ++i)
            {
                var item = collector.items[i];
                if (item.Key.Equals(key))
                    return item.Data;
            }
            return default;
        }

        public static bool TryFind(TKey key, out TItem data)
        {
            var collector = GetInstance();
            for (int i = 0; i < collector.items.Count; ++i)
            {
                var item = collector.items[i];
                if (item.Key.Equals(key))
                {
                    data = item.Data;
                    return true;
                }
            }
            data = default;
            return false;
        }

        private static DictionaryDataCollectorT<TCollector, TItem, TKey> GetInstance()
        {
            if (instance == null)
            {
                instance = ExcelDataManager.instance.FindCollectors<TCollector>();
                if (instance == null)
                {
                    instance = CreateInstance<TCollector>();
                }
            }
            return instance as DictionaryDataCollectorT<TCollector, TItem, TKey>;
        }

        private void Awake()
        {
            instance = this;
            var sheetAttribute = typeof(TItem).GetCustomAttribute<ExcelSheetAttribute>();
            if (sheetAttribute == null)
            {
                throw new Exception($"导出类型必须使用 ExcelSheetAttribute 标记需要导出的页签名字规则 : {typeof(TItem).Name}");
            }
            var keyInfo = typeof(TItem).GetCustomAttribute<KeyColumnAttribute>();
            if (keyInfo == null)
                throw new Exception($"导出类型必须使用 KeyColumnAttribute 标记需要导出Key对应的列名字 : {typeof(TItem).Name}");
            SheetName = sheetAttribute.Name;
            KeyName = keyInfo.Name;
            ExcelDataManager.instance.OnDataCollectorCreate(this);
        }

        private void OnDestroy()
        {
            instance = null;
        }

        internal override void ReadFromFile(string filePath)
        {
            string json = System.IO.File.ReadAllText(filePath);
            var sheet = JsonUtility.FromJson<SheetData>(json);
            IColumnReader reader = ColumnReaderUtil.ToRead<TItem>();
            var keyRead = new ColumnReader(ConvertUtil.ToConvert(typeof(TKey)), KeyName);
            for (int i = 0; i < sheet.Data.Count; ++i)
            {
                try
                {
                    var data = reader.Read(sheet, i);
                    var key = (TKey)keyRead.Read(sheet, i);
                    items.Add(new ItemeData() { Key = key, Data = (TItem)data });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"导出时出错 {sheet.Name} -> 行:{sheet.Data[i].RowIndex}\n{ex.Message}\n{ex.StackTrace}");
                    break;
                }
            }
        }

        protected virtual string GetShowName(TItem item)
        {
            return string.Empty;
        }


    }
}
