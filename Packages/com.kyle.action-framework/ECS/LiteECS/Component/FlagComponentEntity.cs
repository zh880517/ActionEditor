namespace ECSLite
{
    internal class FlagComponentEntity<T> where T : class, IComponent, new()
    {
        public int EntityIdx;
        public int Index;

        public void Reset()
        {
            EntityIdx = default;
        }
    }
}
