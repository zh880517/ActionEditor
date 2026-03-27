using System.Collections.Generic;
using UnityEngine;
namespace NamedAsset.Editor
{
    public abstract class DependencePackPolicy : ScriptableObject
    {
        public string Description;

        /// <summary>
        /// 依赖文件打包策略
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="files">需要被分包的依赖文件</param>
        /// <returns>未打包的文件，进入下一个流程处理</returns>
        public abstract List<string> PackDependence(AssetPackageBuilder builder, List<string> files);

        protected static string GetFolderName(string file)
        {
            int lastSlash = file.LastIndexOf('/');
            int lastBackSlash = file.LastIndexOf('/', lastSlash + 1);
            return file.Substring(lastBackSlash + 1, lastBackSlash - lastSlash - 1);
        }

        protected static string GetFolderBundleName(string file)
        {
            int lastBackSlash = file.LastIndexOf('/');
            int startIndex = file.IndexOf("Assets/") + 7;
            return file.Substring(startIndex, lastBackSlash - startIndex).Replace('/', '_');
        }
    }
}
