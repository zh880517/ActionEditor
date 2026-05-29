namespace ECSLite
{
    public struct Group<IContext, TComponent> where TComponent : class, IContext, IComponent, new()
    {
        private ContextT<IContext> context;
        private EntityFindResult<IContext, TComponent> result;
        public readonly Entity<IContext> Entity=>result.Entity;
        public readonly TComponent Component=>result.Component;

        public Group(ContextT<IContext> context)
        {
            result = new EntityFindResult<IContext, TComponent>();
            this.context = context;
        }
        public bool MoveNext()
        {
            result = context.Find<TComponent>(result.Index);
            return result.Component != null;
        }
        public void Reset()
        {
            result = new EntityFindResult<IContext, TComponent>();
        }
    }
}
