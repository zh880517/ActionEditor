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

    internal class AssetBundleInfo
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
        public bool HasAssetLoaded { get; private set; }
        public bool HasAsset => AssetNames != null;

        public bool IsDone => State > BundleLoadState.Loading && DependenceIdx.Length == DepnedenceComplateCount;

        public void Unload()
        {
            Bundle.Unload(true);
            Bundle = null;
            State = default;
        }
    }
}
