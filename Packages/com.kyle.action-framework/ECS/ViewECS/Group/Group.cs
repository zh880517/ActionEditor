namespace VECS
{
    public struct EntityFindResult<TComponent> where TComponent : IViewComponent
    {
        public ViewEntity Entity;
        public int Index;
        public ulong Version;
        public TComponent Component;
    }

    public struct Group<TComponent> where TComponent : class, IViewComponent, new()
    {
        private readonly ViewContext Context;
        private EntityFindResult<TComponent> Result;
        private readonly bool InCludeDisable;
        public readonly ViewEntity Entity=>Result.Entity;
        public readonly TComponent Component=>Result.Component;
        public Group(ViewContext context, bool inCludeDisable)
        {
            Result = new EntityFindResult<TComponent>();
            InCludeDisable = inCludeDisable;
            Context = context;
        }

        public bool MoveNext()
        {
            Result = Context.Find<TComponent>(Result.Index, 0, InCludeDisable);
            return Result.Entity != null;
        }

        public void Reset()
        {
            Result = new EntityFindResult<TComponent>();
        }
    }

}