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

    internal class AssetBundleInfo
    {
        public string Path;
        public Hash128 Hash;
        public int Index;
        public uint Crc;
        public int[] DependenceIdx;
        public string[] AssetNames;
        public BundleLoadState State;
        public AssetBundle Bundle;
        public bool HasAsset => AssetNames != null;

        public void Unload()
        {
            if (Bundle != null)
            {
                Bundle.Unload(true);
                Bundle = null;
            }
            State = BundleLoadState.None;
        }
    }
}
