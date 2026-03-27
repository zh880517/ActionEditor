using UnityEngine;

namespace NamedAsset
{
    internal abstract class BundleLoadTask
    {
        public AssetBundleInfo Info;
        public abstract bool IsDone { get; }
        public abstract AssetBundle GetAssetBundle();
    }
}
