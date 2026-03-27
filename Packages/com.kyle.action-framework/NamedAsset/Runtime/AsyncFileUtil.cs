using System.IO;
using System.Collections;
using UnityEngine.Networking;

namespace NamedAsset
{
    internal class AsyncFileUtil
    {
        public static IEnumerable ReadAssetManifest(FileLocaltion file, System.Action<AssetManifest> onResult)
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
                yield return webRequest.SendWebRequest();
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
            if (string.IsNullOrEmpty(json))
            {
                AssetManifest manifest = new AssetManifest();
                UnityEngine.JsonUtility.FromJsonOverwrite(json, manifest);
                onResult(manifest);
            }
            else
            {
                onResult(null);
            }
        }

        internal static BundleLoadTask LoadAssetBundle(FileLocaltion file, AssetBundleInfo info)
        {
            switch (file.Type)
            {
                case FilePathType.File:
                case FilePathType.CombinFile:
            	    return new FileBundleLoadTask(info, file.Path, file.Offset);
                case FilePathType.URL:
                    return new HttpBundleLoadTask(file.Path, info);
                case FilePathType.Bytes:
                    return new MemoryBundleLoadTask(info, file.Data);
            }
            return null;
        }
    }
}
