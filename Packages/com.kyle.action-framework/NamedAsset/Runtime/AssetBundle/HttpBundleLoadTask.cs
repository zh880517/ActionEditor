using UnityEngine;
using UnityEngine.Networking;

namespace NamedAsset
{
    internal class HttpBundleLoadTask : BundleLoadTask
    {
        private readonly UnityWebRequest webRequest;
        public override bool IsDone => webRequest.isDone;

        public override AssetBundle GetAssetBundle()
        {
            return DownloadHandlerAssetBundle.GetContent(webRequest);
        }
        public HttpBundleLoadTask(string url, AssetBundleInfo info)
        {
            Info = info;
            webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, info.Hash, 0);
            webRequest.SendWebRequest();
        }
    }
}
