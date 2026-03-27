using UnityEngine;

namespace NamedAsset
{
    internal class FileBundleLoadTask : BundleLoadTask
    {
        private readonly AssetBundleCreateRequest createRequest;
        public override bool IsDone => createRequest.isDone;

        public override AssetBundle GetAssetBundle()
        {
            return createRequest.assetBundle;
        }

        public FileBundleLoadTask(AssetBundleInfo info, string path, ulong offset = 0)
        {
            Info = info;
            createRequest = AssetBundle.LoadFromFileAsync(path, info.Crc, offset);
        }
    }
}
