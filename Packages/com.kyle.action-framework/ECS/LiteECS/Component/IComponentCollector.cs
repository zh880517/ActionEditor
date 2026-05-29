namespace ECSLite
{
    internal interface IComponentCollector
    {
        int Count { get; }
        IComponent Add(int entityID);
        IComponent Get(int entityID);
        void Remove(int entityID);
        void RemoveAll();
    }
    internal interface IComponentCollectorT<T> : IComponentCollector where T : class, IComponent, new()
    {
        ComponentFindResult<T> Find(int startIndex);
        ComponentFindResult<T> MatchFind<TMatcher>(int startIndex, TMatcher matcher) where TMatcher : IComponentMatcher<T>;
    }

}