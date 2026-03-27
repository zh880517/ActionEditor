using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    internal class AssetBundleProvider : IAssetProvider
    {
        public static int MaxLoadBundleCount = 10;

        /// <summary>
        /// 资源缓存条目（struct避免GC，通过字典重新赋值更新）
        /// </summary>
        private struct AssetCacheEntry
        {
            public Object Asset;
            public int RefCount;
            public int BundleIndex;
        }

        private readonly List<AssetBundleInfo> bundleInfos = new List<AssetBundleInfo>();
        private readonly Dictionary<string, int> assetLocation = new Dictionary<string, int>();
        private readonly Queue<AssetBundleInfo> bundleQueue = new Queue<AssetBundleInfo>();
        private readonly Dictionary<int, AssetCacheEntry> assetCache = new Dictionary<int, AssetCacheEntry>();
        private readonly IPathProvider pathProvider;
        private int loadingCount;

        // 复用集合，ClearUnusedAssets时使用避免每次分配
        private byte[] bundleFlags;
        private readonly List<int> tempRemoveKeys = new List<int>();

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
                assetLocation.Add(item.Name, item.Location);
            }

            BuildBundleInfo(manifest);
            bundleFlags = new byte[bundleInfos.Count];
        }

        public async Awaitable<AssetLoadResult> LoadAsset<T>(string name) where T : Object
        {
            if (!assetLocation.TryGetValue(name, out int location))
            {
                return new AssetLoadResult { Result = AssetRequestResult.AssetNotFound };
            }

            // 缓存命中：直接增加引用计数并返回
            if (assetCache.TryGetValue(location, out var cached))
            {
                if (cached.Asset is T)
                {
                    cached.RefCount++;
                    assetCache[location] = cached; // struct写回
                    return new AssetLoadResult { Asset = cached.Asset, Location = location, Result = AssetRequestResult.Success };
                }
                return new AssetLoadResult { Result = AssetRequestResult.AssetLoadFailed };
            }

            int bundleIdx = location >> 16;
            int assetIdx = location & 0xFFFF;
            var info = bundleInfos[bundleIdx];

            // 优先加载依赖Bundle
            for (int i = 0; i < info.DependenceIdx.Length; i++)
            {
                int depIdx = info.DependenceIdx[i];
                if (depIdx >= 0)
                {
                    await EnsureBundleLoaded(bundleInfos[depIdx]);
                }
            }

            // 加载目标Bundle
            await EnsureBundleLoaded(info);

            if (info.State != BundleLoadState.Loaded || info.Bundle == null)
            {
                return new AssetLoadResult { Result = AssetRequestResult.BundleLoadFailed };
            }

            // 并发加载同一资源时，等待结束后可能已被缓存
            if (assetCache.TryGetValue(location, out var existingEntry))
            {
                if (existingEntry.Asset is T)
                {
                    existingEntry.RefCount++;
                    assetCache[location] = existingEntry;
                    return new AssetLoadResult { Asset = existingEntry.Asset, Location = location, Result = AssetRequestResult.Success };
                }
                return new AssetLoadResult { Result = AssetRequestResult.AssetLoadFailed };
            }

            // 从Bundle加载资源
            string assetName = info.AssetNames[assetIdx];
            var assetReq = info.Bundle.LoadAssetAsync<T>(assetName);
            await assetReq;
            T asset = assetReq.asset as T;

            // await之后再次检查（另一个协程可能已经缓存了同一资源）
            if (assetCache.TryGetValue(location, out var raceEntry))
            {
                if (raceEntry.Asset is T)
                {
                    raceEntry.RefCount++;
                    assetCache[location] = raceEntry;
                    return new AssetLoadResult { Asset = raceEntry.Asset, Location = location, Result = AssetRequestResult.Success };
                }
            }

            if (asset == null)
            {
                return new AssetLoadResult { Result = AssetRequestResult.AssetLoadFailed };
            }

            assetCache[location] = new AssetCacheEntry { Asset = asset, RefCount = 1, BundleIndex = bundleIdx };
            return new AssetLoadResult { Asset = asset, Location = location, Result = AssetRequestResult.Success };
        }

        public void Release(int location)
        {
            if (assetCache.TryGetValue(location, out var entry))
            {
                entry.RefCount--;
                if (entry.RefCount < 0) entry.RefCount = 0;
                assetCache[location] = entry; // struct写回
            }
        }

        public void ClearUnusedAssets()
        {
            if (bundleFlags == null) return;
            System.Array.Clear(bundleFlags, 0, bundleFlags.Length);

            // 标记仍有引用的Bundle及其依赖
            foreach (var kv in assetCache)
            {
                if (kv.Value.RefCount > 0)
                {
                    MarkBundleActive(kv.Value.BundleIndex);
                }
            }

            // 收集引用计数为0的缓存条目
            tempRemoveKeys.Clear();
            foreach (var kv in assetCache)
            {
                if (kv.Value.RefCount <= 0)
                {
                    tempRemoveKeys.Add(kv.Key);
                }
            }
            for (int i = 0; i < tempRemoveKeys.Count; i++)
            {
                assetCache.Remove(tempRemoveKeys[i]);
            }
            tempRemoveKeys.Clear();

            // 卸载没有活跃引用的Bundle
            for (int i = 0; i < bundleInfos.Count; i++)
            {
                if (bundleFlags[i] == 0 && bundleInfos[i].State == BundleLoadState.Loaded)
                {
                    bundleInfos[i].Unload();
                }
            }
        }

        private void MarkBundleActive(int index)
        {
            if (index < 0 || index >= bundleFlags.Length || bundleFlags[index] != 0) return;
            bundleFlags[index] = 1;
            var deps = bundleInfos[index].DependenceIdx;
            for (int i = 0; i < deps.Length; i++)
            {
                if (deps[i] >= 0)
                {
                    MarkBundleActive(deps[i]);
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
            // 构建BundleName到索引的映射，避免后续O(N)查找
            var bundleNameToIndex = new Dictionary<string, int>(manifest.Bundles.Count);
            for (int i = 0; i < manifest.Bundles.Count; ++i)
            {
                var bundle = manifest.Bundles[i];
                AssetBundleInfo info = new AssetBundleInfo
                {
                    Index = i,
                    Path = bundle.Name,
                    Hash = bundle.Hash,
                    Crc = bundle.Crc
                };
                int depCount = bundle.Dependencies != null ? bundle.Dependencies.Length : 0;
                info.DependenceIdx = new int[depCount];
                if (bundle.Assets != null && bundle.Assets.Length > 0)
                {
                    info.AssetNames = bundle.Assets;
                }
                bundleInfos.Add(info);
                bundleNameToIndex[bundle.Name] = i;
            }
            // 解析依赖索引
            for (int i = 0; i < bundleInfos.Count; ++i)
            {
                var info = bundleInfos[i];
                var deps = manifest.Bundles[i].Dependencies;
                if (deps == null) continue;
                for (int j = 0; j < deps.Length; ++j)
                {
                    info.DependenceIdx[j] = bundleNameToIndex.TryGetValue(deps[j], out int idx) ? idx : -1;
                }
            }
        }

        public void Destroy()
        {
            assetCache.Clear();
            for (int i = 0; i < bundleInfos.Count; i++)
            {
                bundleInfos[i].Unload();
            }
            bundleInfos.Clear();
            bundleFlags = null;
        }
    }
}
