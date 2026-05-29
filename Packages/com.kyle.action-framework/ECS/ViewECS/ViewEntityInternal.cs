using System.Collections;
using UnityEngine;

namespace VECS
{
    internal class ViewEntityInternal
    {
        public enum EntityState
        {
            None,
            Active,//
            Loaded,//加载完成
        }
        public EntityState State;
        public int Index;
        public int Version;
        public ViewContext Owner;
        public GameObject Object;
        public BitArray ComponentFlag;

        public ViewEntity ToEntity()
        {
            return new ViewEntity { entity = this, Index = Index, Version = Version };
        }

        public T AddComponent<T>(bool forceModify) where T : class, IViewComponent, new()
        {
            var component = Owner.AddComponent<T>(this, forceModify);
            if (!ViewComponentIdentity<T>.Unique)
                ComponentFlag[ViewComponentIdentity<T>.Id] = true;
            return component;
        }

        public T GetComponent<T>() where T : class, IViewComponent, new()
        {
            int componentId = ViewComponentIdentity<T>.Id;
            if (ComponentFlag[componentId])
            {
                return Owner.GetComponent<T>(this) as T;
            }
            return null;
        }

        public T ModifyComponent<T>() where T : class, IViewComponent, new()
        {
            int componentId = ViewComponentIdentity<T>.Id;
            if (ComponentFlag[componentId])
            {
                return Owner.ModifyComponent<T>(this) as T;
            }
            return null;
        }

        public bool HasComponent<T>() where T : class, IViewComponent, new()
        {
            if (!ViewComponentIdentity<T>.Unique)
                return ComponentFlag[ViewComponentIdentity<T>.Id];
            return Owner.GetComponent<T>(this) != null;
        }

        public void RemoveComponent<T>() where T : class, IViewComponent, new()
        {
            int componentId = ViewComponentIdentity<T>.Id;
            if (ComponentFlag[componentId])
            {
                Owner.RemoveComponent<T>(this);
                ComponentFlag[componentId] = false;
            }
        }

        public void Clear()
        {
            State = EntityState.None;
            Version++;
            Object = null;
            ComponentFlag.SetAll(false);
        }
    }
}
