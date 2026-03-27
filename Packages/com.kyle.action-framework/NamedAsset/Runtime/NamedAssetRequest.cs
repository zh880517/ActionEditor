using UnityEngine;

namespace NamedAsset
{
    public enum AssetLoadState
    {
        None,
        Loading,
        NoneExist,
        LoadFailed,
        Loaded,
    }

    internal interface IAssetOwner
    {
        void ReleaseAsset(NamedAssetRequest request);
    }

    internal class NamedAssetRequest : CustomYieldInstruction
    {
        internal IAssetOwner owner;
        internal int refCount;
        internal int Version;
        internal Object asset;
#if UNITY_EDITOR
        internal bool isPrefab;
#endif
        private System.Action<Object> onComplete;
        public AssetLoadState State { get; internal set; }
        public override bool keepWaiting => State <= AssetLoadState.Loading;

        public event System.Action<Object> OnComplete
        {
            add
            {
                if (State > AssetLoadState.Loading )
                {
                    value(asset);
                }
                else
                {
                    onComplete += value;
                }
            }
            remove
            {
                onComplete -= value;
            }
        }

        internal void SetAsset(Object asset)
        {
            //如果状态被设置为None，表示已经被释放了
            if (State == AssetLoadState.None)
                return;
            this.asset = asset;
            State = asset ? AssetLoadState.Loaded : AssetLoadState.LoadFailed;
            var action = onComplete;
            onComplete = null;
            action?.Invoke(asset);

        }

        public GameObject Instantiate(Transform parent)
        {
#if UNITY_EDITOR
            if (isPrefab)
            {
                return UnityEditor.PrefabUtility.InstantiatePrefab(asset as GameObject, parent) as GameObject;
            }
#endif
            if (asset is GameObject go)
            {
                return Object.Instantiate(go, parent);
            }
            return null;
        }

        public T GetAsset<T>() where T : Object
        {
            return asset as T;
        }

        public void Release()
        {
            --refCount;
            if (refCount <= 0)
            {
                owner?.ReleaseAsset(this);
            }
        }

        internal void Unload()
        {
            State = AssetLoadState.None;
            asset = null;
            onComplete = null;
            refCount = 0;
            Version++;
        }

        public static readonly NamedAssetRequest NoneExist = new NamedAssetRequest { State = AssetLoadState.NoneExist };
    }
}
