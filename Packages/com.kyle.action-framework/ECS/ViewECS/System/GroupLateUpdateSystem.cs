namespace VECS
{
    public abstract class GroupLateUpdateSystem<TComponent> : ILateUpdateSystem where TComponent : class, IViewComponent, new()
    {
        private readonly bool includeDisable;
        protected ViewContext mContext;
        public GroupLateUpdateSystem(ViewContext context, bool includeDisable = false)
        {
            mContext = context;
            this.includeDisable = includeDisable;
        }
        public void OnLateUpdate()
        {
            var group = mContext.CreatGroup<TComponent>(includeDisable);
            while (group.MoveNext())
            {
                OnExecuteEntity(group.Entity, group.Component);
            }
        }
        protected abstract void OnExecuteEntity(ViewEntity entity, TComponent component);
    }

}