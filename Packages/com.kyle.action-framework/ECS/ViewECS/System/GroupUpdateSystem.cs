namespace VECS
{
    //处理所有拥有指定Component的Entity
    public abstract class GroupUpdateSystem<TComponent> : IUpdateSystem where TComponent : class, IViewComponent, new()
    {
        private readonly bool includeDisable;
        protected ViewContext mContext;
        public GroupUpdateSystem(ViewContext context, bool includeDisable = false)
        {
            mContext = context;
            this.includeDisable = includeDisable;
        }
        public void OnUpdate()
        {
            OnStartUpdate();
            var group = mContext.CreatGroup<TComponent>(includeDisable);
            while (group.MoveNext())
            {
                OnExecuteEntity(group.Entity, group.Component);
            }
            OnFinishUpdate();
        }

        protected virtual void OnStartUpdate() { }
        protected virtual void OnFinishUpdate() { }
            

        protected abstract void OnExecuteEntity(ViewEntity entity, TComponent component);
    }
}