namespace VECS
{
    internal interface IViewComponentCollector
    {
        int Count { get; }
        IViewComponent Add(ViewEntityInternal entity, ulong version, bool forceModify);
        IViewComponent Get(ViewEntityInternal entity);
        void Remove(ViewEntityInternal entity);
        void RemoveAll();
        IViewComponent Modify(ViewEntityInternal entity, ulong version);
    }
    internal interface IComponentCollectorT<T> : IViewComponentCollector where T : class, IViewComponent, new()
    {
        EntityFindResult<T> Find(int startIndex, ulong version, bool includeDisable);
        EntityFindResult<T> MatchFind<TMatcher>(int startIndex, ulong version, bool includeDisable, TMatcher matcher) where TMatcher : IViewComponentMatcher<T>;
    }
    internal class ComponentEntity<T> where T : class, IViewComponent, new()
    {
        public T Component = new T();
        public ViewEntityInternal Owner;
        public int Index;
        public ulong Version;

        public void Reset()
        {
            Owner = null;
            Version = 0;
            ViewComponentClear<T>.OnReset(Component);
        }
    }

    internal class FlagComponentEntity<T> where T : class, IViewComponent, new()
    {
        public ViewEntityInternal Owner;
        public int Index;
        public ulong Version;

        public void Reset()
        {
            Owner = null;
            Version = 0;
        }
    }
}
