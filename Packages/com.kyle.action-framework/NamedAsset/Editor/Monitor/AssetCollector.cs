using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NamedAsset.Editor
{
    public class AssetCollector : ScriptableSingleton<AssetCollector>, IAssetPackageInfoProvider
    {
        [System.Serializable]
        public struct AssetRef
        {
            public string Name;
            public string Path;
        }
        [System.Serializable]
        public class Package
        {
            public string Name;
            public bool IsDirty;
            public List<AssetRef> Assets = new List<AssetRef>();
        }
        public List<Package> Packages = new List<Package>();
        [SerializeField]
        private int version = 1;
        public int Version => version;


        private readonly Dictionary<string, string> namedAssets = new Dictionary<string, string>();

        private void Awake()
        {
            ForceRefresh();
        }

        private void OnEnable()
        {
            AssetImportMonitor.Enable = true;
            Refresh();
        }
        private void OnDisable()
        {
            AssetImportMonitor.Enable = false;
        }

        private void RefreshPackage(Package package, AssetPackage setting)
        {
            package.IsDirty = false;
            var assets = setting.GetAssetPaths();
            for (int i = 0; i < assets.Length; ++i)
            {
                if (!package.Assets.Exists(it=>it.Path == assets[i]))
                {
                    AssetRef assetRef = new AssetRef
                    {
                        Name = $"{setting.Name}/{System.IO.Path.GetFileNameWithoutExtension(assets[i])}",
                        Path = assets[i]
                    };
                    package.Assets.Add(assetRef);
                }
            }
        }

        private void RefreshNamedAssets()
        {
            namedAssets.Clear();
            foreach (var p in Packages)
            {
                foreach (var a in p.Assets)
                {
                    if (!namedAssets.ContainsKey(a.Name))
                    {
                        namedAssets.Add(a.Name, a.Path);
                    }
                }
            }
        }

        private void AssetNameCheck(Package package)
        {
            for (int i = 0; i < package.Assets.Count; ++i)
            {
                var asset = package.Assets[i];
                for (int j = i+1; j < package.Assets.Count; ++j)
                {
                    if (asset.Name == package.Assets[j].Name)
                    {
                        Debug.LogError($"打包资源重名 {asset.Name}, 打包设置 {package.Name}, 重名资源: \n{asset.Path}\n{package.Assets[j]}");
                    }
                }
            }
        }

        public string GetAssetPath(string assetKey)
        {
            if (namedAssets.TryGetValue(assetKey, out string path))
            {
                return path;
            }
            return null;
        }

        public T LoadAsset<T>(string assetKey) where T : Object
        {
            if (!string.IsNullOrEmpty(assetKey))
            {
                var path = GetAssetPath(assetKey);
                if (!string.IsNullOrEmpty(path))
                {
                    return AssetDatabase.LoadAssetAtPath<T>(path);
                }
            }
            return null;
        }

        public string AssetPathToKey(string path)
        {
            foreach (var kv in namedAssets)
            {
                if (kv.Value == path)
                {
                    return kv.Key;
                }
            }
            return null;
        }

        public string AssetToKey(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var kv in namedAssets)
                {
                    if (kv.Value == path)
                    {
                        return kv.Key;
                    }
                }
            }
            if (asset)
            {
                Debug.LogError($"资源未打包 : {asset.name} => {path}");
            }
            return null;
        }

        public void ForceRefresh()
        {
            Packages.Clear();
            Refresh();
        }

        public void Refresh()
        {
            var settingPackages = AssetPackSetting.instance.Packages;
            for (int i=Packages.Count-1; i>=0; --i)
            {
                var package = Packages[i];
                int index = settingPackages.FindIndex(p => p.Name == package.Name);
                if (index < 0)
                {
                    Packages.RemoveAt(i);
                    continue;
                }
                var setting = settingPackages[index];
                if (string.IsNullOrEmpty(setting.Name)
                    || string.IsNullOrEmpty(setting.Path)
                    || string.IsNullOrEmpty(setting.SearchPattern))
                {
                    Packages.RemoveAt(i);
                    continue;
                }
                if (package.IsDirty)
                {
                    package.Assets.Clear();
                    while (index >= 0)
                    {
                        RefreshPackage(package, settingPackages[index]);
                        index = settingPackages.FindIndex(index+1, p => p.Name == package.Name);
                    }
                }
            }
            for (int i=0; i<settingPackages.Count; ++i)
            {
                var setting = settingPackages[i];
                if (string.IsNullOrEmpty(setting.Name)
                    || string.IsNullOrEmpty(setting.Path)
                    || string.IsNullOrEmpty(setting.SearchPattern))
                    continue;
                var package = Packages.Find(p => p.Name == setting.Name);
                if (package == null)
                {
                    package = new Package();
                    package.Name = setting.Name;
                    RefreshPackage(package, setting);
                    Packages.Add(package);
                    for (int j=i+1; j<settingPackages.Count; ++j)
                    {
                        if (settingPackages[j].Name == setting.Name)
                        {
                            RefreshPackage(package, settingPackages[j]);
                        }
                    }
                }
            }
            for (int i = Packages.Count - 1; i >= 0; --i)
            {
                AssetNameCheck(Packages[i]);
            }

            RefreshNamedAssets();
            version++;
        }

        public Dictionary<string, string> GetAllAssets()
        {
            Refresh();
            return namedAssets;
        }
        [RuntimeInitializeOnLoadMethod]
        static void OnInit()
        {
            //AssetCollector在不访问的时候就不创建
            //如果是强制使用AssetBundle加载，就不需要AssetCollector
            if (!EditorPrefs.GetBool("_forceBundle_"))
            {
                AssetManager.PackageInfo = instance;
            }
        }
    }
}
