namespace ECSLite
{
    public class SystemT<ContextType, IContext> : ISystem
        where ContextType : ContextT<IContext>, new()
    {
        protected ContextType Context { get; private set; }

        public SystemT(ContextType context)
        {
            Context = context;
        }

        public Group<IContext, T> CreateGroup<T>() where T : class, IContext, IComponent, new()
        {
            return Context.CreateGroup<T>();
        }
    }
}
