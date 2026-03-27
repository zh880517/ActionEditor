using System.Collections.Generic;
using UnityEditor;

namespace NamedAsset.Editor
{
    internal class AssetImportMonitor : AssetPostprocessor
    {
        public static bool Enable;
        public static bool waitRefresh = false;
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!Enable)
                return;
            var packages = AssetPackSetting.instance.Packages;
            HashSet<string> dirtyPackages = new HashSet<string>();
            for (int i = 0; i < importedAssets.Length; ++i)
            {
                string packageName = FindPackage(importedAssets[i], packages);
                if (packageName != null)
                {
                    dirtyPackages.Add(packageName);
                }
            }
            for (int i = 0; i < deletedAssets.Length; ++i)
            {
                string packageName = FindPackage(deletedAssets[i], packages);
                if (packageName != null)
                {
                    dirtyPackages.Add(packageName);
                }
            }
            for (int i = 0; i < movedAssets.Length; ++i)
            {
                string packageName = FindPackage(movedAssets[i], packages);
                if (packageName != null)
                {
                    dirtyPackages.Add(packageName);
                }
            }
            for (int i = 0; i < movedFromAssetPaths.Length; ++i)
            {
                string packageName = FindPackage(movedFromAssetPaths[i], packages);
                if (packageName != null)
                {
                    dirtyPackages.Add(packageName);
                }
            }
            foreach (var packageName in dirtyPackages)
            {
                var package = AssetCollector.instance.Packages.Find((p) => p.Name == packageName);
                if (package != null)
                {
                    package.IsDirty = true;
                }
            }
            if (dirtyPackages.Count > 0)
            {
                waitRefresh = true;
                EditorApplication.delayCall += () =>
                {
                    waitRefresh = false;
                    AssetCollector.instance.Refresh();
                };
            }
        }

        private static string FindPackage(string assetPath, List<AssetPackage> packages)
        {
            for (int i=0; i<packages.Count; ++i)
            {
                var package = packages[i];
                if (assetPath.StartsWith(package.Path))
                {
                    return package.Name;
                }
            }
            return null;
        }
    }
}
