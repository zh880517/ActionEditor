namespace ECSLite
{
    internal class ComponentEntity<T> where T : class, IComponent, new()
    {
        public T Component = new T();
        public int EntityIdx = -1;
        public int Index;
        public int PreviousIndex = -1;
        public int NextIndex = -1;
        public ulong Version;

        public void Reset()
        {
            EntityIdx = -1;
            PreviousIndex = -1;
            NextIndex = -1;
            Version = 0;
            ComponentReset<T>.OnReset(Component);
        }
    }
}
