using UnityEngine;

namespace NamedAsset
{
    internal class MemoryBundleLoadTask : BundleLoadTask
    {
        private readonly AssetBundleCreateRequest createRequest;
        public override bool IsDone => createRequest.isDone;

        public override AssetBundle GetAssetBundle()
        {
            return createRequest.assetBundle;
        }

        public MemoryBundleLoadTask(AssetBundleInfo info, byte[] bytes)
        {
            Info = info;
            createRequest = AssetBundle.LoadFromMemoryAsync(bytes, info.Crc);
        }
    }
}
