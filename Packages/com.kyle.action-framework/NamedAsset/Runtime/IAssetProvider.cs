using UnityEngine;

namespace NamedAsset
{
    /// <summary>
    /// Provider加载资产后返回的结果，由AssetManager包装为带Handle的AssetRequest
    /// </summary>
    internal struct AssetLoadResult
    {
        public Object Asset;
        public int Location;
        public AssetRequestResult Result;
    }

    internal interface IAssetProvider
    {
        Awaitable Initialize();
        Awaitable<AssetLoadResult> LoadAsset<T>(string name) where T : Object;
        /// <summary>
        /// 减少指定location的引用计数
        /// </summary>
        void Release(int location);
        /// <summary>
        /// 卸载所有引用计数为0的资源及其Bundle
        /// </summary>
        void ClearUnusedAssets();
        void Destroy();
    }
}
