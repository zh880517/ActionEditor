using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyConfig.Editor
{
    public static class EditorListConfig<T> where T : IListConfig
    {
        private static bool loaded;
        private static ConfigTypeData typeData;

        public static int Count
        {
            get
            {
                EnsureLoaded();
                return ConfigListCollector<T>.Configs.Count;
            }
        }

        public static IReadOnlyList<T> Configs
        {
            get
            {
                EnsureLoaded();
                return ConfigListCollector<T>.Configs;
            }
        }

        public static T Get(int index)
        {
            EnsureLoaded();
            return ConfigListCollector<T>.Configs[index];
        }

        public static int FindIndex(Predicate<T> match)
        {
            EnsureLoaded();
            return ConfigListCollector<T>.Configs.FindIndex(match);
        }

        public static int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            EnsureLoaded();
            return ConfigListCollector<T>.Configs.FindIndex(startIndex, count, match);
        }

        public static T Find(Predicate<T> match)
        {
            EnsureLoaded();
            return ConfigListCollector<T>.Configs.Find(match);
        }

        public static bool Exists(Predicate<T> match)
        {
            EnsureLoaded();
            return ConfigListCollector<T>.Configs.Exists(match);
        }

        private static void EnsureLoaded()
        {
            if (loaded)
                return;
            if (!TryGetTypeData(out var data))
                return;

            try
            {
                ExportUtil.Read<T>(ExcelDataManager.CachePath);
                EditorExcelConfigLinkUtility.LinkPrimary(data);
                EditorExcelConfigReloadDispatcher.RegisterConfigType(typeof(T));
                loaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorListConfig] 读取 Editor Excel 缓存失败：{typeof(T).FullName}\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        private static bool TryGetTypeData(out ConfigTypeData data)
        {
            if (typeData != null)
            {
                data = typeData;
                return true;
            }

            data = null;
            if (!ExcelBinaryTypeCollector.TryCreateTypeData(typeof(T), out var result))
                return false;
            if (!result.IsListConfig)
            {
                Debug.LogError($"[EditorListConfig] 类型 {typeof(T).FullName} 不是 List 或 LinkedList 配置，不能通过 EditorListConfig 访问。");
                return false;
            }
            if (string.IsNullOrEmpty(result.SheetName))
            {
                Debug.LogError($"[EditorListConfig] 类型 {typeof(T).FullName} 缺少 ExcelSheetAttribute，不能读取 Editor Excel 缓存。");
                return false;
            }

            typeData = result;
            data = typeData;
            return true;
        }

        internal static void ClearLoadedForTests()
        {
            loaded = false;
            typeData = null;
        }
    }
}
