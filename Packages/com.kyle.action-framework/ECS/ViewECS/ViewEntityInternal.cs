using System.Collections;
using System.Collections.Generic;
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
        public readonly List<int> ComponentIds = new List<int>();

        public ViewEntity ToEntity()
        {
            return new ViewEntity { entity = this, Index = Index, Version = Version };
        }

        public T AddComponent<T>(bool forceModify) where T : class, IViewComponent, new()
        {
            var component = Owner.AddComponent<T>(this, forceModify);
            int componentId = ViewComponentIdentity<T>.Id;
            if (ViewComponentIdentity<T>.Unique)
            {
                AddComponentId(componentId);
            }
            else if (!ComponentFlag[componentId])
            {
                ComponentFlag[componentId] = true;
                AddComponentId(componentId);
            }
            return component;
        }

        public T GetComponent<T>() where T : class, IViewComponent, new()
        {
            int componentId = ViewComponentIdentity<T>.Id;
            if (ViewComponentIdentity<T>.Unique)
            {
                return Owner.GetComponent<T>(this) as T;
            }
            if (ComponentFlag[componentId])
            {
                return Owner.GetComponent<T>(this) as T;
            }
            return null;
        }

        public T ModifyComponent<T>() where T : class, IViewComponent, new()
        {
            int componentId = ViewComponentIdentity<T>.Id;
            if (ViewComponentIdentity<T>.Unique)
            {
                return Owner.ModifyComponent<T>(this) as T;
            }
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
            if (ViewComponentIdentity<T>.Unique)
            {
                if (Owner.GetComponent<T>(this) != null)
                {
                    Owner.RemoveComponent<T>(this);
                }
                RemoveComponentId(componentId);
                return;
            }
            if (ComponentFlag[componentId])
            {
                Owner.RemoveComponent<T>(this);
                ComponentFlag[componentId] = false;
                RemoveComponentId(componentId);
            }
        }

        public void AddComponentId(int componentId)
        {
            if (!ComponentIds.Contains(componentId))
            {
                ComponentIds.Add(componentId);
            }
        }

        public void RemoveComponentId(int componentId)
        {
            ComponentIds.Remove(componentId);
        }

        public void Clear()
        {
            State = EntityState.None;
            Version++;
            Object = null;
            ComponentFlag.SetAll(false);
            ComponentIds.Clear();
        }
    }
}
