using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EasyConfig.Editor
{
    public static class EditorExcelConfigReloadDispatcher
    {
        private static readonly HashSet<Type> registeredTypes = new HashSet<Type>();
        private static readonly Dictionary<Type, ConfigTypeData> typeDataByType = new Dictionary<Type, ConfigTypeData>();
        private static readonly Dictionary<string, List<ConfigTypeData>> typeDataBySheet = new Dictionary<string, List<ConfigTypeData>>();
        private static readonly MethodInfo ReadListMethod = typeof(EditorExcelConfigReloadDispatcher).GetMethod(nameof(ReadListConfig), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ReadDictionaryMethod = typeof(EditorExcelConfigReloadDispatcher).GetMethod(nameof(ReadDictionaryConfig), BindingFlags.NonPublic | BindingFlags.Static);

        public static IReadOnlyCollection<Type> RegisteredConfigTypes => registeredTypes;

        public static void RegisterConfigType(Type configType)
        {
            if (configType == null)
                return;
            if (registeredTypes.Contains(configType))
                return;
            if (!ExcelBinaryTypeCollector.TryCreateTypeData(configType, out var typeData))
                return;
            if (string.IsNullOrEmpty(typeData.SheetName))
            {
                Debug.LogError($"[EditorExcelConfigReloadDispatcher] 类型 {configType.FullName} 缺少 ExcelSheetAttribute，不能注册 Editor Excel 热刷新。");
                return;
            }

            registeredTypes.Add(configType);
            typeDataByType.Add(configType, typeData);
            if (!typeDataBySheet.TryGetValue(typeData.SheetName, out var sheetTypes))
            {
                sheetTypes = new List<ConfigTypeData>();
                typeDataBySheet.Add(typeData.SheetName, sheetTypes);
            }
            sheetTypes.Add(typeData);
        }

        public static void UnregisterConfigType(Type configType)
        {
            if (configType == null || !registeredTypes.Remove(configType))
                return;
            if (!typeDataByType.TryGetValue(configType, out var typeData))
                return;

            typeDataByType.Remove(configType);
            if (!typeDataBySheet.TryGetValue(typeData.SheetName, out var sheetTypes))
                return;

            sheetTypes.RemoveAll(it => it.Type == configType);
            if (sheetTypes.Count == 0)
                typeDataBySheet.Remove(typeData.SheetName);
        }

        public static void NotifyModify(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
                return;
            if (!typeDataBySheet.TryGetValue(sheetName, out var sheetTypes))
                return;

            var snapshot = sheetTypes.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (!snapshot[i].IsLinkedConfig)
                    Refresh(snapshot[i]);
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].IsLinkedConfig)
                    Refresh(snapshot[i]);
            }
        }

        public static void Clear()
        {
            registeredTypes.Clear();
            typeDataByType.Clear();
            typeDataBySheet.Clear();
        }

        private static void Refresh(ConfigTypeData typeData)
        {
            try
            {
                if (typeData.IsListConfig)
                {
                    ReadListMethod.MakeGenericMethod(typeData.Type).Invoke(null, null);
                }
                else
                {
                    ReadDictionaryMethod.MakeGenericMethod(typeData.KeyType, typeData.Type).Invoke(null, null);
                }
                EditorExcelConfigLinkUtility.LinkPrimary(typeData);
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                Debug.LogError($"[EditorExcelConfigReloadDispatcher] 刷新 Editor Excel 缓存失败：{typeData.Type.FullName}\n{inner.Message}\n{inner.StackTrace}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorExcelConfigReloadDispatcher] 刷新 Editor Excel 缓存失败：{typeData.Type.FullName}\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void ReadListConfig<T>() where T : IListConfig
        {
            ExportUtil.Read<T>(ExcelDataManager.CachePath);
        }

        private static void ReadDictionaryConfig<TKey, T>() where TKey : struct where T : IDictionaryConfig
        {
            ExportUtil.Read<TKey, T>(ExcelDataManager.CachePath);
        }

        internal static void ClearForTests()
        {
            Clear();
        }
    }
}
