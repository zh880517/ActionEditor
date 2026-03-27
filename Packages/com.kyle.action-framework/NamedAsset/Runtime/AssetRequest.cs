namespace NamedAsset
{
    public enum AssetRequestResult
    {
        Succee,
        BundleUnExist,
        BundleLoadFailed,
        AssetUnExist,
        AssetLoadFailed,
    }

    public struct AssetRequest<T> where T : UnityEngine.Object
    {
        internal int KeyIndex;
        internal T Value;
        public AssetRequestResult Result;
        public string Name;
        public readonly T Asset=> Value;
        public readonly void Release()
        {

        }
    }
}
