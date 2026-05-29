namespace VECS
{
    public interface IViewComponentMatcher<T> where T :IViewComponent
    {
        bool Match(ViewEntity entity, T component);
    }

    public struct MatchGroup<TComponent, TMatcher> where TComponent : class, IViewComponent, new() where TMatcher : IViewComponentMatcher<TComponent>
    {
        private readonly ViewContext Context;
        private EntityFindResult<TComponent> Result;
        private readonly bool InCludeDisable;
        private readonly TMatcher Matcher;
        public readonly ViewEntity Entity => Result.Entity;
        public readonly TComponent Component => Result.Component;

        public MatchGroup(ViewContext context, TMatcher matcher, bool inCludeDisable)
        {
            Matcher = matcher;
            Result = new EntityFindResult<TComponent>();
            InCludeDisable = inCludeDisable;
            Context = context;
        }
        public bool MoveNext()
        {
            Result = Context.MatchFind<TComponent, TMatcher>(Matcher, Result.Index, 0, InCludeDisable);
            return Result.Entity != null;
        }

        public void Reset()
        {
            Result = new EntityFindResult<TComponent>();
        }
    }
}
