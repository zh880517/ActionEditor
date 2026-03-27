using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace NamedAsset.Editor
{
    [FilePath("ProjectSettings/AssetPackSetting.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AssetPackSetting : ScriptableSingleton<AssetPackSetting>
    {
        //允许不同的Package重名，但是不能有相同的资源名
        //重复的资源会被过滤掉
        public List<AssetPackage> Packages = new List<AssetPackage>();

        //从上往下，上一部未处理的文件会进入下一步处理
        [Header("依赖文件分包策略")]
        public List<DependencePackPolicy> DependencePackPolicies = new List<DependencePackPolicy>();
        [Header("打包配置")]
        public string PackExportPath = "StreamingAssets/bundle/";
        public string BundleExternName = ".ab";
        public BuildAssetBundleOptions BuildOptions = BuildAssetBundleOptions.ChunkBasedCompression;


        public void Save()
        {
            Save(true);
        }

        public void Build()
        {
            AssetPackageBuilder builder = new AssetPackageBuilder();
            builder.Build(PackExportPath);
        }
    }
}