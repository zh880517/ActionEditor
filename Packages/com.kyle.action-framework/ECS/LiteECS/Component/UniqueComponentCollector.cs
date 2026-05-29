namespace ECSLite
{
    internal class UniqueComponentCollector<T> : IComponentCollectorT<T> where T : class, IUniqueComponent, new()
    {
        private readonly ComponentEntity<T> Component = new ComponentEntity<T>();
        public int Count => Component.EntityIdx >= 0 ? 1 : 0;

        public IComponent Add(int entityID)
        {
            if (Component.EntityIdx == entityID)
            {
                return Component.Component;
            }
            else
            {
                Remove(entityID);
            }
            Component.EntityIdx = entityID;
            return Component.Component;
        }
        public T TryGet(out int entityIdx)
        {
            entityIdx = Component.EntityIdx;
            if (Component.EntityIdx < 0)
                return null;
            return Component.Component;
        }

        public ComponentFindResult<T> Find(int startIndex)
        {
            var result = new ComponentFindResult<T>() { Index = 1};
            if (startIndex == 0
                && Component.EntityIdx >= 0 )
            {
                result.Component = Component.Component;
                result.Index = 1;
                result.EntityIndex = Component.EntityIdx;
            }
            return result;
        }

        public ComponentFindResult<T> MatchFind<TMatcher>(int startIndex, TMatcher matcher) where TMatcher : IComponentMatcher<T>
        {
            var result = new ComponentFindResult<T>() { Index = 1 };
            if (startIndex == 0
                && Component.EntityIdx >= 0
                && matcher.Match(Component.Component))
            {
                result.Component = Component.Component;
                result.Index = 1;
                result.EntityIndex = Component.EntityIdx;
            }
            return result;
        }
        public IComponent Get(int entityIdx)
        {
            if (entityIdx == Component.EntityIdx)
                return Component.Component;
            return null;
        }

        public void Remove(int entityIdx)
        {
            if (entityIdx == Component.EntityIdx)
            {
                Component.Reset();
            }
        }

        public void RemoveAll()
        {
            if (Component.EntityIdx >= 0)
            {
                Component.Reset();
            }
        }
    }
}
