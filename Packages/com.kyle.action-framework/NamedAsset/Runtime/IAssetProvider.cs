using UnityEngine;

namespace NamedAsset
{
    internal interface IAssetProvider
    {
        Awaitable Initialize();
        Awaitable<AssetRequest<T>> LoadAsset<T>(string name) where T : Object;

        void Destroy();
    }
}
