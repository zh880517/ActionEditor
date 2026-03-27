using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    internal class AssetBundleProvider : IAssetProvider, IBundleOwner
    {
        public static int MaxLoadBundleCount = 10;

        private readonly List<AssetBundleInfo> bundleInfos = new List<AssetBundleInfo>();
        private readonly Dictionary<string, int> assetLoaction = new Dictionary<string, int>();
        private readonly Queue<AssetBundleInfo> bundleQueue = new Queue<AssetBundleInfo>();
        private readonly Dictionary<int, AssetCacheEntry> assetCache = new Dictionary<int, AssetCacheEntry>();
        private readonly IPathProvider pathProvider;
        private int loadingCount;

        private class AssetCacheEntry
        {
            public Object Asset;
            public int RefCount;
        }

        public AssetBundleProvider(IPathProvider pathProvider)
        {
            this.pathProvider = pathProvider;
        }

        public async Awaitable Initialize()
        {
            var file = pathProvider.GetAssetManifestPath();
            var manifest = await AsyncFileUtil.ReadAssetManifest(file);
            if (manifest == null)
            {
                throw new System.Exception($"load AssetManifest fail : {file.Path} => empty data");
            }
            foreach (var item in manifest.Assets)
            {
                assetLoaction.Add(item.Name, item.Location);
            }

            BuildBundleInfo(manifest);
        }

        public async Awaitable<AssetRequest<T>> LoadAsset<T>(string name) where T : Object
        {
            if (!assetLoaction.TryGetValue(name, out int location))
            {
                return new AssetRequest<T> { Name = name, Result = AssetRequestResult.AssetUnExist };
            }

            // Check cache first
            if (assetCache.TryGetValue(location, out var cached))
            {
                T cachedAsset = cached.Asset as T;
                if (cachedAsset != null)
                {
                    cached.RefCount++;
                    return new AssetRequest<T> { Name = name, Value = cachedAsset, Result = AssetRequestResult.Succee };
                }
                else
                {
                    return new AssetRequest<T> { Name = name, Result = AssetRequestResult.AssetLoadFailed };
                }
            }

            int bundleIdx = location >> 16;
            int assetIdx = location & 0xFFFF;
            var info = bundleInfos[bundleIdx];

            // Load dependency bundles first
            for (int i = 0; i < info.DependenceIdx.Length; i++)
            {
                int depIdx = info.DependenceIdx[i];
                if (depIdx >= 0)
                {
                    await EnsureBundleLoaded(bundleInfos[depIdx]);
                }
            }

            // Load the main bundle
            await EnsureBundleLoaded(info);

            if (info.State == BundleLoadState.LoadFailed || info.Bundle == null)
            {
                return new AssetRequest<T> { Name = name, Result = AssetRequestResult.BundleLoadFailed };
            }

            // Load the asset from the bundle
            string assetName = info.AssetNames[assetIdx];
            var assetReq = info.Bundle.LoadAssetAsync<T>(assetName);
            await assetReq;
            T asset = assetReq.asset as T;
            if (asset == null)
            {
                assetCache[location] = new AssetCacheEntry { Asset = asset, RefCount = 0 };
                return new AssetRequest<T> { Name = name, Result = AssetRequestResult.AssetLoadFailed };
            }

            // Cache the loaded asset
            assetCache[location] = new AssetCacheEntry { Asset = asset, RefCount = 1 };
            return new AssetRequest<T> { Name = name, Value = asset, Result = AssetRequestResult.Succee };
        }

        public void ReleaseAsset(string name)
        {
            if (assetLoaction.TryGetValue(name, out int location))
            {
                if (assetCache.TryGetValue(location, out var entry))
                {
                    entry.RefCount--;
                }
            }
        }

        public void ClearUnusedAsset()
        {
            // Collect which bundles still have in-use assets
            var activeBundles = new HashSet<int>();
            foreach (var kv in assetCache)
            {
                if (kv.Value.RefCount > 0)
                {
                    activeBundles.Add(kv.Key >> 16);
                }
            }

            // Mark bundles that are dependencies of active bundles
            var protectedBundles = new HashSet<int>(activeBundles);
            foreach (int idx in activeBundles)
            {
                MarkDependencies(idx, protectedBundles);
            }

            // Remove unused cache entries and unload unprotected bundles
            var removeKeys = new List<int>();
            var unloadBundles = new HashSet<int>();
            foreach (var kv in assetCache)
            {
                int bundleIdx = kv.Key >> 16;
                if (!protectedBundles.Contains(bundleIdx))
                {
                    removeKeys.Add(kv.Key);
                    unloadBundles.Add(bundleIdx);
                }
            }
            foreach (int key in removeKeys)
            {
                assetCache.Remove(key);
            }
            foreach (int idx in unloadBundles)
            {
                var info = bundleInfos[idx];
                if (info.State == BundleLoadState.Loaded)
                {
                    info.Unload();
                }
            }
        }

        private void MarkDependencies(int bundleIdx, HashSet<int> set)
        {
            var deps = bundleInfos[bundleIdx].DependenceIdx;
            for (int i = 0; i < deps.Length; i++)
            {
                int dep = deps[i];
                if (dep >= 0 && set.Add(dep))
                {
                    MarkDependencies(dep, set);
                }
            }
        }

        private async Awaitable EnsureBundleLoaded(AssetBundleInfo info)
        {
            if (info.State == BundleLoadState.None)
            {
                if (loadingCount >= MaxLoadBundleCount)
                {
                    info.State = BundleLoadState.InQueue;
                    bundleQueue.Enqueue(info);
                }
                else
                {
                    await LoadBundle(info);
                }
            }
            while (info.State == BundleLoadState.InQueue || info.State == BundleLoadState.Loading)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        private async Awaitable LoadBundle(AssetBundleInfo info)
        {
            loadingCount++;
            var path = pathProvider.GetAssetBundlePath(info.Path);
            await AsyncFileUtil.LoadAssetBundleAsync(path, info);
            loadingCount--;
            FlushQueue();
        }

        private void FlushQueue()
        {
            while (loadingCount < MaxLoadBundleCount && bundleQueue.Count > 0)
            {
                var next = bundleQueue.Dequeue();
                if (next.State == BundleLoadState.InQueue)
                {
                    _ = LoadBundle(next);
                }
            }
        }


        private void BuildBundleInfo(AssetManifest manifest)
        {
            for (int i=0; i< manifest.Bundles.Count; ++i)
            {
                var bundle = manifest.Bundles[i];
                AssetBundleInfo info = new AssetBundleInfo
                {
                    Owner = this,
                    Index = i,
                    Path = bundle.Name,
                    Hash = bundle.Hash,
                    Crc = bundle.Crc
                };
                int depCount = 0;
                if (bundle.Dependencies != null && bundle.Dependencies.Length > 0)
                {
                    depCount = bundle.Dependencies.Length;
                }
                info.DependenceIdx = new int[depCount];
                if (bundle.Assets != null && bundle.Assets.Length > 0)
                {
                    info.AssetNames = bundle.Assets;
                }
                bundleInfos.Add(info);
            }
            for (int i=0; i<bundleInfos.Count; ++i)
            {
                var info = bundleInfos[i];
                var bundle = manifest.Bundles[i];
                for (int j = 0; j < bundle.Dependencies.Length; ++j)
                {
                    var idx = bundleInfos.FindIndex(it => it.Path == bundle.Dependencies[j]);
                    info.DependenceIdx[j] = idx;
                }
            }
        }

        public void Destroy()
        {
            assetCache.Clear();
            foreach (var bundle in bundleInfos)
            {
                bundle.Unload();
            }
            bundleInfos.Clear();
        }

        public void ReleaseBundle(AssetBundleInfo bundle)
        {
        }

    }
}
