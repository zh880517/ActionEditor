namespace ECSLite
{
    public interface IComponentMatcher<T> where T: IComponent
    {
        bool Match(T component);
    }

    public struct MatchGroup<IContext, TComponent, TMatcher> where TComponent : class, IContext, IComponent, new() where TMatcher : IComponentMatcher<TComponent>
    {
        private ContextT<IContext> context;
        private EntityFindResult<IContext, TComponent> result;
        private TMatcher matcher;
        public readonly Entity<IContext> Entity=>result.Entity;
        public readonly TComponent Component=>result.Component;

        public MatchGroup(ContextT<IContext> context, TMatcher matcher)
        {
            result = new EntityFindResult<IContext, TComponent>();
            this.context = context;
            this.matcher = matcher;
        }
        public bool MoveNext()
        {
            result = context.MatchFind<TComponent, TMatcher>(result.Index, matcher);
            return result.Component != null;
        }
        public void Reset()
        {
            result = new EntityFindResult<IContext, TComponent>();
        }
    }
}
