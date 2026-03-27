using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
namespace NamedAsset.Editor
{
    public class BundleInfo
    {
        public string Name;
        public List<string> Assets = new List<string>();
        public bool IsDependence;//是否是依赖包
    }

    public class AssetPackageBuilder
    {
        public readonly List<BundleInfo> BundleInfos = new List<BundleInfo>();
        public readonly Dictionary<string, string> NamedAssets = new Dictionary<string, string>();
        private readonly Dictionary<string, string> fileInBundle = new Dictionary<string, string>();
        private readonly string externalName;
        private string exportPath;

        public AssetPackageBuilder()
        {
            externalName = AssetPackSetting.instance.BundleExternName;

            var packages = AssetPackSetting.instance.Packages;
            foreach (var p in packages)
            {
                var files = p.GetAssetPaths();
                for (int i=0; i<files.Length; ++i)
                {
                    var filePath = files[i];
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    string assetName = $"{p.Name}/{fileName}";
                    if (NamedAssets.TryGetValue(assetName, out string existPath))
                    {
                        if (existPath != filePath)
                        {
                            throw new System.Exception($"资源重名打包失败: {assetName} -> {existPath} <=> {filePath}");
                        }
                    }
                    else
                    {
                        NamedAssets.Add(assetName, filePath);
                    }
                }
                switch (p.PackType)
                {
                    case AssetPackType.PackAllInOne:
                        AddAssetsToBundle(files, p.Name, false);
                	    break;
                    case AssetPackType.PackDirectory:
                        foreach (var file in files)
                        {
                            int lastSlash = file.LastIndexOf('/');
                            int lastBackSlash = file.LastIndexOf('/', lastSlash + 1);
                            AddAssetToBundle(file, $"{p.Name}/{file.Substring(lastBackSlash + 1, lastBackSlash - lastSlash - 1)}", false);
                        }
                        break;
                    case AssetPackType.PackSingleFile:
                        foreach (var file in files)
                        {
                            var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                            AddAssetToBundle(file, $"{p.Name}/{fileName}", false);
                        }
                        break;
                }
            }
        }

        public void PackDepenceFile(string assetPath, string bundleName)
        {
            AddAssetToBundle(assetPath, $"depend/{bundleName}", true);
        }

        public void Build(string targetPath)
        {
            if (BundleInfos.Count == 0)
                return;
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);
            exportPath = targetPath;
            if (!exportPath.EndsWith('/'))
                exportPath += '/';
            HashSet<string> allDependence = new HashSet<string>();
            int step = 0;
            foreach (var kv in fileInBundle)
            {
                EditorUtility.DisplayProgressBar("资源依赖分析", kv.Key, (float)++step / fileInBundle.Count);
                CollectionDependence(kv.Key, allDependence);
            }
            EditorUtility.ClearProgressBar();

            //依赖文件分包
            List<string> allDependenceList = new List<string>(allDependence);
            foreach (var policy in AssetPackSetting.instance.DependencePackPolicies)
            {
                if (allDependenceList == null || allDependenceList.Count == 0)
                    break;
                allDependenceList = policy.PackDependence(this, allDependenceList);
            }
            if (allDependenceList != null && allDependenceList.Count > 0)
            {
                AddAssetsToBundle(allDependenceList, "dependence_other", true);
            }
            //打包
            AssetBundleBuild[] assetBundleBuilds = BundleInfos.Select((b) =>
            {
                return new AssetBundleBuild
                {
                    assetBundleName = b.Name,
                    assetNames = b.Assets.ToArray()
                };
            }).ToArray();
            var resutlt = BuildPipeline.BuildAssetBundles(targetPath, assetBundleBuilds, AssetPackSetting.instance.BuildOptions, EditorUserBuildSettings.activeBuildTarget);
            if (resutlt != null)
            {

                ExportManifest(Path.Combine(targetPath, "AssetManifest.json"), resutlt);
                HashSet<string> bundles = new HashSet<string>(resutlt.GetAllAssetBundlesWithVariant());
                var files = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (!fileName.EndsWith(externalName) || fileName.EndsWith(".json"))
                        continue;
                    string partName = fileName.Replace(targetPath, "").Replace('\\', '/');
                    if (!bundles.Contains(partName))
                    {
                        File.Delete(file);
                        string metaPath = file + ".meta";
                        if (File.Exists(metaPath))
                            File.Delete(metaPath);
                        string manifest = file + ".Manifest";
                        if (File.Exists(manifest))
                            File.Delete(manifest);
                        string dir = Path.GetDirectoryName(file);
                        if (Directory.GetFiles(dir).Length == 0)
                            Directory.Delete(dir);
                    }
                }
            }
        }

        private void ExportManifest(string path, UnityEngine.AssetBundleManifest bundleManifest)
        {
            //运行时使用AssetManifest不使用AssetBundleManifest，减少一次异步加载文件的开销
            //打包时可以删除AssetBundleManifest
            AssetManifest assetManifest = new AssetManifest();
            //记录指定打包资源所在的Bundle
            foreach (var bundle in BundleInfos)
            {
                var allDeps = bundleManifest.GetAllDependencies(bundle.Name);
                AssetManifest.BundleInfo bundleInfo = new AssetManifest.BundleInfo
                {
                    Name = bundle.Name,
                    Dependencies = allDeps,
                    Hash = bundleManifest.GetAssetBundleHash(bundle.Name),
                };
                string abPath = exportPath + bundle.Name;
                if (BuildPipeline.GetCRCForAssetBundle(abPath, out uint crc))
                {
                    bundleInfo.Crc = crc;
                }
                if (!bundle.IsDependence)
                    bundleInfo.Assets = bundle.Assets.ToArray();
                assetManifest.Bundles.Add(bundleInfo);
            }
            //记录资源所在Bundle的索引
            foreach (var kv in NamedAssets)
            {
                string bundleName = fileInBundle[kv.Value];
                int bundleIdx = BundleInfos.FindIndex((b) => b.Name == bundleName);
                int assetIdx = BundleInfos[bundleIdx].Assets.IndexOf(kv.Value);
                AssetManifest.AssetInfo assetInfo = new AssetManifest.AssetInfo
                {
                    Name = kv.Key,
                    Location = bundleIdx << 16 | assetIdx
                };
                assetManifest.Assets.Add(assetInfo);
            }
            File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(assetManifest), new System.Text.UTF8Encoding(false));
        }

        private static readonly HashSet<string> s_NonePackFiles = new HashSet<string>
        {
            ".cs", ".dll", ".so"
        };

        private string FormatBundleName(string bundleName)
        {
            if (bundleName.Contains(' '))
            {
                bundleName = bundleName.Replace(' ', '_');
            }
            if (!string.IsNullOrEmpty(externalName))
            {
                bundleName += externalName;
            }
            return bundleName;
        }

        private void CollectionDependence(string assetPath, HashSet<string> allDependence)
        {
            var deps = AssetDatabase.GetDependencies(assetPath, true);
            foreach (var dep in deps)
            {
                var extension = Path.GetExtension(dep).ToLower();
                if (!string.IsNullOrEmpty(extension) && !s_NonePackFiles.Contains(extension))
                    allDependence.Add(dep);
            }
        }
        
        private void AddAssetToBundle(string assetPath, string bundleName, bool isDependence)
        {
            bundleName = FormatBundleName(bundleName);
            if (fileInBundle.ContainsKey(assetPath))
                return;
            var bundleInfo = BundleInfos.Find((b) => b.Name == bundleName);
            if (bundleInfo == null)
            {
                bundleInfo = new BundleInfo();
                bundleInfo.Name = bundleName;
                bundleInfo.IsDependence = isDependence;
                BundleInfos.Add(bundleInfo);
            }
            if (!bundleInfo.Assets.Contains(assetPath))
            {
                bundleInfo.Assets.Add(assetPath);
                fileInBundle.Add(assetPath, bundleName);
            }
        }
        
        private void AddAssetsToBundle(IEnumerable<string> assetPaths, string bundleName, bool isDependence)
        {
            bundleName = FormatBundleName(bundleName);
            var bundleInfo = BundleInfos.Find((b) => b.Name == bundleName);
            if (bundleInfo == null)
            {
                bundleInfo = new BundleInfo();
                bundleInfo.Name = bundleName;
                bundleInfo.IsDependence = isDependence;
                BundleInfos.Add(bundleInfo);
            }
            foreach (var asset in assetPaths)
            {
                if (fileInBundle.ContainsKey(asset))
                    continue;
                if (!bundleInfo.Assets.Contains(asset))
                {
                    bundleInfo.Assets.Add(asset);
                    fileInBundle.Add(asset, bundleName);
                }
            }
        }

    }
}
