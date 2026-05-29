namespace ECSLite
{
    internal class FlagComponentEntity<T> where T : class, IComponent, new()
    {
        public int EntityIdx = -1;
        public int Index;

        public void Reset()
        {
            EntityIdx = -1;
        }
    }
}
