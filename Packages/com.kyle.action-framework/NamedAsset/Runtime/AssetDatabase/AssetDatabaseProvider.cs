using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    internal class AssetDatabaseProvider : IAssetProvider
    {
        public static int MaxLoadAssetCount = 10;
        private Dictionary<string, string> assetPaths;
        private readonly Dictionary<string, int> nameToLocation = new Dictionary<string, int>();
        private readonly Dictionary<int, Object> locationToAsset = new Dictionary<int, Object>();
        private int nextLocation;

        public AssetDatabaseProvider()
        {
#if UNITY_EDITOR
            assetPaths = AssetManager.PackageInfo?.GetAllAssets() ?? new Dictionary<string, string>();
#endif
        }

        public async Awaitable Initialize()
        {
            await Awaitable.EndOfFrameAsync();
        }

        public async Awaitable<AssetLoadResult> LoadAsset<T>(string name) where T : Object
        {
#if UNITY_EDITOR
            // 已加载过：返回相同location（AssetManager会创建新Handle用于独立Release）
            if (nameToLocation.TryGetValue(name, out int existingLoc))
            {
                if (locationToAsset.TryGetValue(existingLoc, out var cached) && cached is T)
                {
                    return new AssetLoadResult { Asset = cached, Location = existingLoc, Result = AssetRequestResult.Success };
                }
            }

            if (assetPaths == null || !assetPaths.TryGetValue(name, out string path))
            {
                return new AssetLoadResult { Result = AssetRequestResult.AssetNotFound };
            }

            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                return new AssetLoadResult { Result = AssetRequestResult.AssetLoadFailed };
            }

            int location = nextLocation++;
            nameToLocation[name] = location;
            locationToAsset[location] = asset;
            await Awaitable.EndOfFrameAsync();
            return new AssetLoadResult { Asset = asset, Location = location, Result = AssetRequestResult.Success };
#else
            throw new System.NotImplementedException();
#endif
        }

        public void Release(int location)
        {
            // Editor模式下AssetDatabase管理资源生命周期，无需手动卸载
        }

        public void ClearUnusedAssets()
        {
            // Editor模式下无需卸载
        }

        public void Destroy()
        {
#if UNITY_EDITOR
            nameToLocation.Clear();
            locationToAsset.Clear();
            assetPaths = null;
#endif
        }
    }
}
