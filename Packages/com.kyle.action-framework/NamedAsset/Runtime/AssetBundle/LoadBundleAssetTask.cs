using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    internal class LoadBundleAssetTask
    {
        public NamedAssetRequest AssetRequest;
        public AssetBundleRequest BundleRequest;

        public  bool IsComplete => BundleRequest != null && BundleRequest.isDone;

        private static Stack<LoadBundleAssetTask> s_pool = new Stack<LoadBundleAssetTask>();

        public void OnFinish()
        {
            if (BundleRequest != null && AssetRequest != null)
            {
                AssetRequest.SetAsset(BundleRequest.asset);
            }

            AssetRequest = null;
            BundleRequest = null;
            s_pool.Push(this);
        }

        public static LoadBundleAssetTask Create(NamedAssetRequest asset, AssetBundle bundle, string name)
        {
            if (!s_pool.TryPop(out LoadBundleAssetTask request))
            {
                request = new LoadBundleAssetTask();
            }
#if UNITY_EDITOR
            asset.isPrefab = false;
#endif
            request.AssetRequest = asset;
            request.BundleRequest = bundle.LoadAssetAsync(name);
            return request;
        }
    }
}
