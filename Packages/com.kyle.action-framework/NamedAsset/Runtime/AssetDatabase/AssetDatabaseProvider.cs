using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    internal class AssetDatabaseProvider : IAssetProvider
    {
        public static int MaxLoadAssetCount = 10;
        private Dictionary<string, string> assetPaths;

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
            if (assetPaths == null || !assetPaths.TryGetValue(name, out string path))
            {
                return new AssetLoadResult { Result = AssetRequestResult.AssetNotFound };
            }

            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                return new AssetLoadResult { Result = AssetRequestResult.AssetLoadFailed };
            }

            await Awaitable.EndOfFrameAsync();
            return new AssetLoadResult { Asset = asset, Location = 0, Result = AssetRequestResult.Success };
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
            assetPaths = null;
#endif
        }
    }
}
