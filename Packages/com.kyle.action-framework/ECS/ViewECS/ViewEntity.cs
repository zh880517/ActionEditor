using System;
namespace VECS
{
    public struct ViewEntity : IEquatable<ViewEntity>
    {
        public int Index;
        public int Version;
        internal ViewEntityInternal entity;
        public readonly bool Valid=> entity!=null && entity.Version == Version;

        public readonly T Add<T>(bool forceModify = false) where T : class, IViewComponent, new()
        {
            if (!Valid)
                return null;
            return entity.AddComponent<T>(forceModify);
        }
        public readonly T Get<T>() where T : class, IViewComponent, new()
        {
            if (!Valid)
                return null;
            return entity.GetComponent<T>();
        }

        public readonly bool TryGet<T>(out T component) where T : class, IViewComponent, new()
        {
            if (!Valid)
            {
                component = null;
                return false;
            }
            component = entity.GetComponent<T>();
            return component != null;
        }
        public readonly bool Has<T>() where T : class, IViewComponent, new()
        {
            if (!Valid)
                return false;
            return entity.GetComponent<T>() != null;
        }
        public readonly T Modify<T>() where T : class, IViewComponent, new()
        {
            if (!Valid)
                return null;
            return entity.ModifyComponent<T>();
        }
        public readonly bool TryModifyGet<T>(out T component) where T : class, IViewComponent, new()
        {
            if (!Valid)
            {
                component = null;
                return false;
            }
            component = entity.ModifyComponent<T>();
            return component != null;
        }
        //Remove时组件会被回收重置，请在读取组件值后再Remove
        public readonly void Remove<T>() where T : class, IViewComponent, new()
        {
            if (!Valid)
                return;
            entity.RemoveComponent<T>();
        }

        public readonly bool Equals(ViewEntity other)
        {
            return other.Version == Version && other.Index == Index;
        }
        public override readonly bool Equals(object obj)
        {
            return obj is ViewEntity other && Equals(other);
        }
        public override readonly int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public static bool operator ==(ViewEntity left, ViewEntity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ViewEntity left, ViewEntity right)
        {
            return !left.Equals(right);
        }

        public static implicit operator bool(ViewEntity entity)
        {
            return entity.Valid;
        }
    }
}