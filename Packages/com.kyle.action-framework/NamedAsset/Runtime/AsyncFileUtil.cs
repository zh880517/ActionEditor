using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NamedAsset
{
    internal class AsyncFileUtil
    {
        public static async Awaitable<AssetManifest> ReadAssetManifest(FileLocaltion file)
        {
            string json = null;
            if (file.Type == FilePathType.File)
            {
                json = File.ReadAllText(file.Path);
            }
            else if (file.Type == FilePathType.CombinFile)
            {
                using(var stream = new FileStream(file.Path, FileMode.Open))
                {
                    var bytes = new byte[file.Length];
                    stream.Read(bytes, (int)file.Offset, (int)file.Length);
                    json = System.Text.Encoding.UTF8.GetString(bytes);
                }
            }
            else if (file.Type == FilePathType.URL)
            {
                UnityWebRequest webRequest = UnityWebRequest.Get(file.Path);
                webRequest.useHttpContinue = false;
                await webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    json = webRequest.downloadHandler.text;
                }
                else
                {
                    throw new System.Exception($"load AssetManifest fail : {file.Path} => {webRequest.error}");
                }
            }
            else if (file.Type == FilePathType.Bytes)
            {
                json = System.Text.Encoding.UTF8.GetString(file.Data);
            }
            if (!string.IsNullOrEmpty(json))
            {
                AssetManifest manifest = new AssetManifest();
                JsonUtility.FromJsonOverwrite(json, manifest);
                return manifest;
            }
            return null;
        }

        internal static async Awaitable LoadAssetBundleAsync(FileLocaltion file, AssetBundleInfo info)
        {
            info.State = BundleLoadState.Loading;
            switch (file.Type)
            {
                case FilePathType.File:
                case FilePathType.CombinFile:
                    {
                        var req = AssetBundle.LoadFromFileAsync(file.Path, info.Crc, file.Offset);
                        await req;
                        info.Bundle = req.assetBundle;
                        info.State = info.Bundle ? BundleLoadState.Loaded : BundleLoadState.LoadFailed;
                    }
                    break;
                case FilePathType.URL:
                    {
                        var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(file.Path, info.Hash, 0);
                        await webRequest.SendWebRequest();
                        info.Bundle = webRequest.result == UnityWebRequest.Result.Success ? DownloadHandlerAssetBundle.GetContent(webRequest) : null;
                        info.State = info.Bundle ? BundleLoadState.Loaded : BundleLoadState.LoadFailed;
                    }
                    break;
                case FilePathType.Bytes:
                    {
                        var req = AssetBundle.LoadFromMemoryAsync(file.Data, info.Crc);
                        info.Bundle = req.assetBundle;
                        info.State = info.Bundle ? BundleLoadState.Loaded : BundleLoadState.LoadFailed;
                    }
                    break;
            }
            return;
        }
    }
}
