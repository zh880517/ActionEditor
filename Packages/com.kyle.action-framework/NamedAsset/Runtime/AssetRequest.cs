namespace NamedAsset
{
    public enum AssetRequestResult
    {
        Success,
        BundleNotFound,
        BundleLoadFailed,
        AssetNotFound,
        AssetLoadFailed,
    }

    /// <summary>
    /// 资源加载请求句柄，基于Version防止重复Release
    /// 当Release后，Asset返回null，IsValid返回false，再次Release不会产生任何效果
    /// </summary>
    public struct AssetRequest<T> where T : UnityEngine.Object
    {
        internal int HandleIndex;
        internal int HandleVersion;
        internal T CachedAsset;
        public AssetRequestResult Result;
        public string Name;

        public readonly T Asset =>
            Result == AssetRequestResult.Success && AssetManager.IsHandleValid(HandleIndex, HandleVersion)
                ? CachedAsset
                : null;

        public readonly bool IsValid =>
            Result == AssetRequestResult.Success && AssetManager.IsHandleValid(HandleIndex, HandleVersion);

        /// <summary>
        /// 释放资源句柄，重复调用不会产生任何效果
        /// </summary>
        public void Release()
        {
            if (AssetManager.ReleaseHandle(HandleIndex, HandleVersion))
            {
                HandleIndex = 0;
                HandleVersion = 0;
                CachedAsset = null;
            }
        }
    }
}
