namespace ECSLite
{
    public struct Entity<IContext> : System.IEquatable<Entity<IContext>>
    {
        internal EntityInternal entity;
        public EntityIdentify ID;
        public readonly bool Valid => entity != null && entity.ID.Version == ID.Version;

        public readonly T Add<T>() where T : class, IContext, IComponent, new()
        {
            if (Valid)
            {
                return entity.AddComponent<T>();
            }
            return null;
        }

        public readonly T Get<T>() where T : class, IContext, IComponent, new()
        {
            if (Valid)
            {
                return entity.GetComponent<T>();
            }
            return null;
        }

        public readonly bool TryGet<T>(out T component) where T : class, IContext, IComponent, new()
        {
            if (Valid)
            {
                component = entity.GetComponent<T>();
                return component != null;
            }
            component = null;
            return false;
        }

        public readonly bool Hast<T>() where T : class, IContext, IComponent, new()
        {
            if (Valid)
                return entity.HasComponent<T>();
            return false;
        }

        public readonly void Remove<T>() where T : class, IContext, IComponent, new()
        {
            if (Valid)
            {
                entity.RemoveComponent<T>();
            }
        }

        public static implicit operator bool(Entity<IContext> entity)
        {
            return entity.Valid;
        }

        public readonly bool Equals(Entity<IContext> other)
        {
            return ID.Equals(other.ID);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is Entity<IContext> other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public static bool operator ==(Entity<IContext> left, Entity<IContext> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity<IContext> left, Entity<IContext> right)
        {
            return !left.Equals(right);
        }
    }
}
