namespace ECSLite
{
    internal class ComponentEntity<T> where T : class, IComponent, new()
    {
        public T Component = new T();
        public int EntityIdx;
        public int Index;
        public ulong Version;

        public void Reset()
        {
            EntityIdx = -1;
            Version = 0;
            ComponentReset<T>.OnReset(Component);
        }
    }
}
