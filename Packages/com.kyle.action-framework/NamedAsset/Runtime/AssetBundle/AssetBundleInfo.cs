using UnityEngine;

namespace NamedAsset
{
    public enum BundleLoadState
    {
        None,
        InQueue,
        Loading,
        LoadFailed,
        Loaded,
    }

    internal interface IBundleOwner
    {
        void ReleaseBundle(AssetBundleInfo bundle);
    }

    internal class AssetBundleInfo : IAssetOwner
    {
        public IBundleOwner Owner;
        public string Path;
        public Hash128 Hash;
        public int Index;
        public uint Crc;
        public int[] DependenceIdx;
        public string[] AssetNames;
        public int DepnedenceComplateCount;
        public BundleLoadState State;
        public AssetBundle Bundle;
        public NamedAssetRequest[] RequestList;
        public bool HasAssetLoaded { get; private set; }
        public bool HasAsset => AssetNames != null;

        public bool IsDone => State > BundleLoadState.Loading && DependenceIdx.Length == DepnedenceComplateCount;

        public NamedAssetRequest GetAssetRequest(int index)
        {
            //这里不做越界检查，如果越界了，说明上层调用有问题
            var request = RequestList[index];
            if (request == null)
            {
                request = new NamedAssetRequest
                {
                    owner = this
                };
                RequestList[index] = request;
            }
            request.refCount++;
            HasAssetLoaded = true;
            return request;
        }

        public void ReleaseAsset(NamedAssetRequest request)
        {
            for (int i = 0; i < RequestList.Length; ++i)
            {
                var r = RequestList[i];
                if (r != null && r.refCount > 0)
                    return;
            }
            HasAssetLoaded = false;
            Owner.ReleaseBundle(this);
        }

        public void Unload()
        {
            if (RequestList != null)
            {
                for (int i = 0; i < RequestList.Length; ++i)
                {
                    var r = RequestList[i];
                    r?.Unload();
                }
            }
            Bundle.Unload(true);
            Bundle = null;
            State = default;
        }
    }
}
