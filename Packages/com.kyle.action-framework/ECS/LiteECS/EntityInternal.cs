using System.Collections;
using System.Collections.Generic;

namespace ECSLite
{
    internal class EntityInternal
    {
        public EntityIdentify ID;
        public bool Used;
        public Context Owner;
        public BitArray ComponentFlag;
        public readonly List<int> ComponentIds = new List<int>();

        public T AddComponent<T>() where T : class, IComponent, new()
        {
            var component = Owner.AddComponent<T>(ID.Index);
            int componentId = ComponentIdentity<T>.Id;
            if (ComponentIdentity<T>.Unique)
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

        public T GetComponent<T>() where T : class, IComponent, new()
        {
            int componentId = ComponentIdentity<T>.Id;
            if (ComponentIdentity<T>.Unique)
            {
                return Owner.GetComponent<T>(ID.Index) as T;
            }
            if (ComponentFlag[componentId])
            {
                return Owner.GetComponent<T>(ID.Index) as T;
            }
            return null;
        }

        public bool HasComponent<T>() where T : class, IComponent, new()
        {
            if (!ComponentIdentity<T>.Unique)
                return ComponentFlag[ComponentIdentity<T>.Id];
            return Owner.GetComponent<T>(ID.Index) != null;
        }

        public void RemoveComponent<T>() where T : class, IComponent, new()
        {
            int componentId = ComponentIdentity<T>.Id;
            if (ComponentIdentity<T>.Unique)
            {
                if (Owner.GetComponent<T>(ID.Index) != null)
                {
                    Owner.RemoveComponent<T>(ID.Index);
                }
                RemoveComponentId(componentId);
                return;
            }
            if (ComponentFlag[componentId])
            {
                Owner.RemoveComponent<T>(ID.Index);
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
            Used = false;
            ID.Version++;
            ComponentFlag.SetAll(false);
            ComponentIds.Clear();
        }
    }
}
