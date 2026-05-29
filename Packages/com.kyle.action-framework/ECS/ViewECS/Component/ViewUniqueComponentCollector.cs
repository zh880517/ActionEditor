using System;
namespace VECS
{
    internal class ViewUniqueComponentCollector<T> : IComponentCollectorT<T> where T : class, IViewUniqueComponent, new()
    {
        public int Count => Component.Owner != null ? 1 : 0;
        private readonly ComponentEntity<T> Component = new ComponentEntity<T>();
        private readonly Action<ViewEntity, T> onRemove = ViewComponentClear<T>.OnRemove;
        public IViewComponent Add(ViewEntityInternal entity, ulong version, bool forceModify)
        {
            if (Component.Owner == entity)
            {
                if (forceModify)
                    Component.Version = version;
                return Component.Component;
            }
            else
            {
                Remove(entity);
            }
            Component.Owner = entity;
            Component.Version = version;
            return Component.Component;
        }

        public EntityFindResult<T> Find(int startIndex, ulong version, bool includeDisable)
        {
            var result = new EntityFindResult<T>() { Index = 1};
            if (startIndex == 0 
                && Component.Owner != null 
                && (Component.Owner.State == ViewEntityInternal.EntityState.Loaded || includeDisable)
                && version < Component.Version)
            {
                result.Component = Component.Component;
                result.Version = Component.Version;
                result.Index = 1;
                result.Entity = Component.Owner.ToEntity();
            }
            return result;
        }

        public EntityFindResult<T> MatchFind<TMatcher>(int startIndex, ulong version, bool includeDisable, TMatcher matcher) where TMatcher : IViewComponentMatcher<T>
        {
            var result = new EntityFindResult<T>() { Index = 1 };
            if (startIndex == 0
                && Component.Owner != null
                && (Component.Owner.State == ViewEntityInternal.EntityState.Loaded || includeDisable)
                && version < Component.Version
                && matcher.Match(Component.Owner.ToEntity(), Component.Component))
            {
                result.Component = Component.Component;
                result.Version = Component.Version;
                result.Index = 1;
                result.Entity = Component.Owner.ToEntity();
            }
            return result;
        }
        public IViewComponent Get(ViewEntityInternal entity)
        {
            if (entity == Component.Owner)
                return Component.Component;
            return null;
        }

        public T TryGet(out ViewEntity entity)
        {
            if (Component.Owner == null)
            {
                entity = default;
                return null;
            }
            entity = Component.Owner.ToEntity();
            return Component.Component;
        }

        public IViewComponent Modify(ViewEntityInternal entity, ulong version)
        {
            if (Component.Owner == entity)
            {
                Component.Version = version;
                return Component.Component;
            }
            return null;
        }

        public void Remove(ViewEntityInternal entity)
        {
            if (entity == Component.Owner)
            {
                onRemove(Component.Owner.ToEntity(), Component.Component);
                Component.Reset();
            }
        }

        public void RemoveAll()
        {
            if (Component.Owner != null)
            {
                onRemove(Component.Owner.ToEntity(), Component.Component);
                Component.Reset();
            }
        }

    }
}