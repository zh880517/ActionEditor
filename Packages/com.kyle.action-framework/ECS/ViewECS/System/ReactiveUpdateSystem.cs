namespace VECS
{
    //仅处理当前帧被修改（Add、Modify）指定Component的Entity
    public abstract class ReactiveUpdateSystem<TComponent> : IUpdateSystem where TComponent : class, IViewComponent, new()
    {
        private readonly int groupId;
        private readonly bool includeDisable;
        protected ViewContext mContext;
        public ReactiveUpdateSystem(ViewContext context, bool includeDisable = false)
        {
            this.includeDisable = includeDisable;
            groupId = context.RegisterReactiveGroup<TComponent>();
            mContext = context;
        }

        public void OnUpdate()
        {
            var group = mContext.GetReactiveGroup<TComponent>(groupId, includeDisable);
            while (group.MoveNext())
            {
                OnExecuteEntity(group.Entity, group.Component);
            }
        }
        protected abstract void OnExecuteEntity(ViewEntity entity, TComponent component);
    }
}