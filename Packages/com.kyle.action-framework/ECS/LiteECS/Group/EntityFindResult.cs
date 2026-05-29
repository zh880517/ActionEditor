namespace ECSLite
{
    internal struct ComponentFindResult<TComponent> where TComponent : IComponent
    {
        public int EntityIndex;
        public int Index;
        public TComponent Component;
    }

    public struct EntityFindResult<IContext, TComponent> where TComponent : IComponent
    {
        public Entity<IContext> Entity;
        public int Index;
        public TComponent Component;
    }

}
