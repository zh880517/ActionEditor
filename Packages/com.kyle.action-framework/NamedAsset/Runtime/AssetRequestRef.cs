using UnityEngine;

namespace NamedAsset
{
    //封装是为了防止重复释放
    public struct AssetRequestRef : System.IEquatable<AssetRequestRef>
    {
        internal int Version;
        internal int KeyIndex;
        public string Name { get; internal set; }
        internal NamedAssetRequest request;
        public readonly CustomYieldInstruction Request => request;
        public readonly bool IsDone => request != null && request.State > AssetLoadState.Loading;
        public readonly bool IsSuccess => request != null && request.State == AssetLoadState.Loaded;
        public readonly bool Valid => request != null && Version == request.Version;
        public event System.Action<Object> OnComplete
        {
            add
            {
                if (request != null)
                    request.OnComplete += value;
            }
            remove
            {
                if (request != null)
                    request.OnComplete -= value;
            }
        }
        public readonly GameObject Instantiate(Transform parent)
        {
            return request?.Instantiate(parent);
        }
        public readonly T GetAsset<T>() where T : Object
        {
            return request?.GetAsset<T>();
        }

        public readonly void Release()
        {
            if (request != null && Version == request.Version)
            {
                //防止重复释放
                if (AssetManager.ReleaseKey(KeyIndex))
                {
                    request.Release();
                }
            }
        }

        public readonly bool Equals(AssetRequestRef other)
        {
            return KeyIndex == other.KeyIndex && Name == other.Name;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is AssetRequestRef other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return Name.GetHashCode() ^ KeyIndex;
        }

        public static bool operator ==(AssetRequestRef left, AssetRequestRef right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssetRequestRef left, AssetRequestRef right)
        {
            return !left.Equals(right);
        }
    }
}
