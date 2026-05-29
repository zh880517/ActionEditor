using System.Collections;

namespace ECSLite
{
    internal class EntityInternal
    {
        public EntityIdentify ID;
        public bool Used;
        public Context Owner;
        public BitArray ComponentFlag;

        public T AddComponent<T>() where T : class, IComponent, new()
        {
            var component = Owner.AddComponent<T>(ID.Index);
            if (!ComponentIdentity<T>.Unique)
                ComponentFlag[ComponentIdentity<T>.Id] = true;
            return component;
        }

        public T GetComponent<T>() where T : class, IComponent, new()
        {
            int componentId = ComponentIdentity<T>.Id;
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
            if (ComponentFlag[componentId])
            {
                Owner.RemoveComponent<T>(ID.Index);
                ComponentFlag[componentId] = false;
            }
        }

        public void Clear()
        {
            Used = false;
            ID.Version++;
            ComponentFlag.SetAll(false);
        }
    }
}
