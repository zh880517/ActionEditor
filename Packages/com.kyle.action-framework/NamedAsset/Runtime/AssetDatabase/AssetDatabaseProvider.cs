using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    internal class AssetDatabaseProvider : IAssetProvider, ITickable
    {
        public static int MaxLoadAssetCount = 10;
        private readonly Dictionary<string, string> assetPaths;
        private readonly Dictionary<string, NamedAssetRequest> assetRequests = new Dictionary<string, NamedAssetRequest>();
        private readonly Queue<string> assetLoadQueue = new Queue<string>();

        public AssetDatabaseProvider()
        {
#if UNITY_EDITOR
            assetPaths = AssetManager.PackageInfo.GetAllAssets();
#endif
        }

        public IEnumerable Initialize()
        {
            yield return new WaitForEndOfFrame();
        }

        public NamedAssetRequest LoadAsset(string name)
        {
#if UNITY_EDITOR
            if (!assetRequests.TryGetValue(name, out var request))
            {
                if (assetPaths.ContainsKey(name))
                {
                    request = new NamedAssetRequest();
                    assetRequests.Add(name, request);
                    assetLoadQueue.Enqueue(name);
                }
                else
                {
                    Debug.LogError($"load asset fail : {name}");
                    request = NamedAssetRequest.NoneExist;
                }
            }
            return request;
#else
            return NamedAssetRequest.NoneExist;
#endif
        }
        public void Destroy()
        {
#if UNITY_EDITOR
            foreach (var kv in assetRequests)
            {
                kv.Value.asset = null;
                kv.Value.State = AssetLoadState.None;
            }
            assetRequests.Clear();
#endif
        }

        public bool OnTick()
        {
#if UNITY_EDITOR
            int count = 0;
            while (count < MaxLoadAssetCount && assetLoadQueue.Count > 0)
            {
                var name = assetLoadQueue.Dequeue();
                if (assetRequests.TryGetValue(name, out var request))
                {
                    var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPaths[name]);
                    request.isPrefab = asset is GameObject;
                    request.SetAsset(asset);
                    ++count;
                }
            }
            return false;
#else
            return true;
#endif
        }
    }
}
