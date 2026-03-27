using System.Collections;
using System.Collections.Generic;

namespace NamedAsset
{
    internal class AssetBundleProvider : IAssetProvider, ITickable, IBundleOwner
    {
        public static int MaxLoadBundleCount = 10;

        private readonly List<AssetBundleInfo> bundleInfos = new List<AssetBundleInfo>();
        private AssetManifest.AssetInfo[] assets;
        private readonly List<LoadBundleAssetTask> assetTasks = new List<LoadBundleAssetTask>();
        private readonly Queue<AssetBundleInfo> bundleQueue = new Queue<AssetBundleInfo>();
        private readonly List<BundleLoadTask> bundleTasks = new List<BundleLoadTask>();
        private readonly IPathProvider pathProvider;
        private bool enableBundleUnload;
        private BitArray bundleFlags;
        private System.Text.StringBuilder logBuilder;

        public AssetBundleProvider(IPathProvider pathProvider)
        {
            this.pathProvider = pathProvider;
        }

        public IEnumerable Initialize()
        {
            var file = pathProvider.GetAssetManifestPath();
            yield return AsyncFileUtil.ReadAssetManifest(file, (manifest) =>
            {
                if (manifest == null)
                {
                    throw new System.Exception($"load AssetManifest fail : {file.Path} => empty data");
                }
                assets = manifest.Assets.ToArray();
                BuildBundleInfo(manifest);
            });
        }

        public NamedAssetRequest LoadAsset(string name)
        {
            for (int i=0; i<assets.Length; ++i)
            {
                if (assets[i].Name == name)
                {
                    int location = assets[i].Location;
                    return LocationToRequest(location);
                }
            }
            return NamedAssetRequest.NoneExist;
        }

        private NamedAssetRequest LocationToRequest(int location)
        {
            int bundleIdx = location >> 16;
            int assetIdx = location & 0xFFFF;
            var info = bundleInfos[bundleIdx];
            CheckAndLoadBundle(info);
            var request = info.GetAssetRequest(assetIdx);
            if (CreateLoadAssetTask(info, assetIdx))
            {
                AssetUpdateLoop.Instance.AddLoadTick(this);
            }
            return request;
        }

        private void CheckAndLoadBundle(AssetBundleInfo info)
        {
            if (info.State < BundleLoadState.InQueue)
            {
                info.State = BundleLoadState.InQueue;
                for (int i=0; i<info.DependenceIdx.Length; ++i)
                {
                    var dep = bundleInfos[info.DependenceIdx[i]];
                    if (dep.State < BundleLoadState.InQueue)
                    {
                        bundleQueue.Enqueue(dep);
                    }
                }
                bundleQueue.Enqueue(info);
                AssetUpdateLoop.Instance.AddLoadTick(this);
            }
        }

        private void BuildBundleInfo(AssetManifest manifest)
        {
            bundleFlags = new BitArray(manifest.Bundles.Count);
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
                    info.RequestList = new NamedAssetRequest[bundle.Assets.Length];
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

        private bool CreateLoadAssetTask(AssetBundleInfo bundleInfo, int idx)
        {
            var request = bundleInfo.RequestList[idx];
            if (request != null && !request.keepWaiting 
                && bundleInfo.IsDone)
            {
                if (bundleInfo.Bundle)
                {
                    var task = LoadBundleAssetTask.Create(request, bundleInfo.Bundle, bundleInfo.AssetNames[idx]);
                    assetTasks.Add(task);
                    return true;
                }
                else
                {
                    //如果bundle加载失败，做加载失败处理
                    //防止卡加载
                    request.SetAsset(null);
                }
            }
            return false;
        }

        private void OnBundleLoadComplete(AssetBundleInfo info)
        {
            foreach (var bundle in bundleInfos)
            {
                if (bundle == info || bundle.IsDone)
                    continue;
                for (int i = 0; i < bundle.DependenceIdx.Length; ++i)
                {
                    if (bundle.DependenceIdx[i] == info.Index)
                    {
                        ++bundle.DepnedenceComplateCount;
                        if (bundle.IsDone)
                        {
                            OnBundleDone(bundle);
                        }
                        break;
                    }
                }
            }
            if (info.IsDone)
            {
                OnBundleDone(info);
            }
        }
        private void OnBundleDone(AssetBundleInfo info)
        {
            for (int i = 0; i < info.RequestList.Length; ++i)
            {
                if (info.RequestList[i] != null)
                {
                    CreateLoadAssetTask(info, i);
                }
            }
        }

        public bool OnTick()
        {
            for (int i=bundleTasks.Count-1; i>=0; --i)
            {
                var task = bundleTasks[i];
                if (task.IsDone)
                {
                    var bundle = task.GetAssetBundle();
                    if (task.Info.State == BundleLoadState.Loading)
                    {
                        task.Info.Bundle = bundle;
                        task.Info.State = bundle ? BundleLoadState.Loaded : BundleLoadState.LoadFailed;
                        OnBundleLoadComplete(task.Info);
                    }
                    else
                    {
                        bundle.Unload(true);
                    }
                    bundleTasks.RemoveAt(i);
                }
            }
            while (bundleTasks.Count < MaxLoadBundleCount && bundleQueue.Count > 0)
            {
                var info = bundleQueue.Dequeue();
                if (info.State == BundleLoadState.None)
                {
                    //如果在队列中，但是状态为None，说明已经被卸载了
                    continue;
                }
                info.State = BundleLoadState.Loading;
                var task = AsyncFileUtil.LoadAssetBundle(pathProvider.GetAssetBundlePath(info.Path), info);
                if (task == null)
                {
                    bundleTasks.Add(task);
                }
                else
                {
                    task.Info.State = BundleLoadState.LoadFailed;
                    OnBundleLoadComplete(info);
                }
            }

            for (int i=assetTasks.Count-1; i>=0; --i)
            {
                var task = assetTasks[i];
                if (task.IsComplete)
                {
                    task.OnFinish();
                    assetTasks.RemoveAt(i);
                }
            }
            UnloadUnUsedBundle();
            return assetTasks.Count == 0 && bundleTasks.Count == 0;
        }

        public void Destroy()
        {
            AssetUpdateLoop.RemoveTick(this);
            foreach (var bundle in bundleInfos)
            {
                bundle.Unload();
            }
            bundleInfos.Clear();
            bundleQueue.Clear();
            bundleTasks.Clear();
            assetTasks.Clear();
        }

        public void ReleaseBundle(AssetBundleInfo bundle)
        {
            enableBundleUnload = true;
            AssetUpdateLoop.Instance.AddLoadTick(this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void RecordUnloadBundle(AssetBundleInfo bundle)
        {
            logBuilder ??= new System.Text.StringBuilder();
            logBuilder.AppendLine(bundle.Path);
        }

        private void UnloadUnUsedBundle()
        {
            if (!enableBundleUnload)
                return;
            enableBundleUnload = false;
            bundleFlags.SetAll(false);
            //标记未使用的bundle(没有指定加载资源的bundle也)
            for (int i=0; i<bundleInfos.Count; ++i)
            {
                var bundle = bundleInfos[i];
                if (!bundle.HasAsset || !bundle.HasAssetLoaded)
                {
                    bundleFlags.Set(i, true);
                }
            }
            //标记依赖的bundle
            for (int i = 0; i < bundleInfos.Count; ++i)
            {
                if (!bundleFlags[i])
                {
                    var bundle = bundleInfos[i];
                    if (bundle.DependenceIdx != null)
                    {
                        for (int j =0; j< bundle.DependenceIdx.Length; ++j)
                        {
                            bundleFlags.Set(bundle.DependenceIdx[j], false);
                        }
                    }
                }
            }
            //卸载未使用的bundle
            int unloadCount = 0;
            for (int i = 0; i < bundleInfos.Count; ++i)
            {
                if (bundleFlags[i])
                {
                    var bundle = bundleInfos[i];
                    if (bundle.State > BundleLoadState.None)
                    {
                        bundle.Unload();
                        unloadCount++;
                        RecordUnloadBundle(bundle);
                    }
                }
            }
            if (unloadCount > 0 && logBuilder != null)
            {
                logBuilder.Insert(0, $"UnloadUnUsedBundleCount = {unloadCount}\n");
                UnityEngine.Debug.Log(logBuilder.ToString());
                logBuilder.Clear();
            }
            //重新计算依赖加载计数
            for (int i = 0; i < bundleInfos.Count; ++i)
            {
                var bundle = bundleInfos[i];
                if (bundle.DependenceIdx != null)
                {
                    for (int j = 0; j < bundle.DependenceIdx.Length; ++j)
                    {
                        if (bundleFlags[bundle.DependenceIdx[j]])
                        {
                            bundle.DepnedenceComplateCount--;
                        }
                    }
                }
            }
        }
    }
}
