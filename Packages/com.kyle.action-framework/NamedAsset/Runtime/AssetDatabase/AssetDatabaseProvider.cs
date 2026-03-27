using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    internal class AssetDatabaseProvider : IAssetProvider
    {
        public static int MaxLoadAssetCount = 10;
        private readonly Dictionary<string, string> assetPaths;
        private readonly Dictionary<string, Object> assetCache;

        public AssetDatabaseProvider()
        {
            assetCache = new Dictionary<string, Object>();
#if UNITY_EDITOR
            assetPaths = AssetManager.PackageInfo.GetAllAssets();
#endif
        }

        public async Awaitable Initialize()
        {
            await Awaitable.EndOfFrameAsync();
        }

        public async Awaitable<AssetRequest<T>> LoadAsset<T>(string name) where T : Object
        {
#if UNITY_EDITOR
            if (assetCache.TryGetValue(name, out var cached))
            {
                if (cached is T cachedAsset)
                {
                    return new AssetRequest<T> { Name = name, Value = cachedAsset, Result = AssetRequestResult.Succee };
                }
            }

            if (!assetPaths.TryGetValue(name, out string path))
            {
                return new AssetRequest<T> { Name = name, Result = AssetRequestResult.AssetUnExist };
            }

            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                return new AssetRequest<T> { Name = name, Result = AssetRequestResult.AssetLoadFailed };
            }

            assetCache[name] = asset;
            await Awaitable.EndOfFrameAsync();
            return new AssetRequest<T> { Name = name, Value = asset, Result = AssetRequestResult.Succee };
#else
            throw new System.NotImplementedException();
#endif
        }

        public void Destroy()
        {
#if UNITY_EDITOR
            assetCache.Clear();
#endif
        }
    }
}
