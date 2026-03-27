using System.IO;
using UnityEditor;
namespace EasyConfig
{
    /// <summary>
    /// 提供一个基于ScriptableObject的数据导出接口
    /// 实现监听Asset的变化，自动导出数据
    /// </summary>
    /// <typeparam name="TAsset"></typeparam>
    public abstract class TAssetExportDataProvider<TAsset> : IEditorDataProvider where TAsset : UnityEngine.ScriptableObject
    {
        protected virtual bool IncludeChildrenType => false;
        public byte[] Load(string type, string name)
        {
            var assetPath = GetAssetPath(type, name);
            var exportPath = AssetExportPath(type, name);
            if (File.Exists(exportPath) && File.Exists(assetPath))
            {
                System.DateTime assetTime = File.GetLastWriteTime(assetPath);
                System.DateTime exportTime = File.GetLastWriteTime(exportPath);
                if (assetTime <= exportTime)
                {
                    return File.ReadAllBytes(exportPath);
                }
            }
            if (assetPath != null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
                if (asset)
                {
                    DoExport(asset);
                    if (File.Exists(exportPath))
                        return File.ReadAllBytes(exportPath);
                }
            }
            return null;
        }


        public virtual bool OnAssetModify(string assetPath)
        {
            if (CheckAssetPath(assetPath))
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                //这里需要判断类型，因为可能有继承关系,如果有特殊需求，可以在子类中重写OnAssetModify
                if (typeof(TAsset).IsAssignableFrom(type) && (IncludeChildrenType || type == typeof(TAsset)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
                    DoExport(asset);
                    return true;
                }
            }
            return false;
        }
        //通知数据修改，重新反序列化并替换缓存，仅在编辑器模式下生效
        public static void OnExportData(string name, string fileName, byte[] data)
        {
            ConfigLoaderManager.OnDataModify(name, fileName, data);
        }
        //如果不需要自动导出，则直接返回false
        //如果路径符合要求，不需要加载内容进行判断，直接返回true,后续会加载并调用导出接口
        protected abstract bool CheckAssetPath(string path);
        //需要在子类中实现，并且触发
        protected abstract void DoExport(TAsset asset);
        //根据类型和导出文件名，获取对应的导出路径
        protected abstract string AssetExportPath(string type, string name);
        //根据类型和导出文件名，获取对应的Asset路径
        protected abstract string GetAssetPath(string type, string name);

        public static string SearchFileInFolder(string folder, string fileName)
        {
            var files = Directory.GetFiles(folder, fileName, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }
            return null;
        }

    }
}
