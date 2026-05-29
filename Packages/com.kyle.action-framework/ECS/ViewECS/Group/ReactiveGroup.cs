namespace VECS
{
    public struct ReactiveGroup<TComponent> where TComponent : class, IViewComponent, new()
    {
        private EntityFindResult<TComponent> Result;
        private readonly int GroupIndex;
        private readonly ulong Version;
        private readonly bool IncludeDisable;
        private readonly ViewContext Context;
        public readonly ViewEntity Entity=> Result.Entity;
        public readonly TComponent Component=> Result.Component;

        public ReactiveGroup(int groupIndex, ulong version, bool includeDisable, ViewContext context)
        {
            Result = new EntityFindResult<TComponent>();
            IncludeDisable = includeDisable;
            Version = version;
            Context = context;
            GroupIndex = groupIndex;
        }

        public bool MoveNext()
        {
            Result = Context.Find<TComponent>(Result.Index, Version, IncludeDisable, GroupIndex);
            return Result.Entity != null;
        }

        public void Reset()
        {
            Result = new EntityFindResult<TComponent>();
        }
    }
}