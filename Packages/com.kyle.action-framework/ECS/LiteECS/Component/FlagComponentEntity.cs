namespace ECSLite
{
    internal class FlagComponentEntity<T> where T : class, IComponent, new()
    {
        public int EntityIdx = -1;
        public int Index;
        public int PreviousIndex = -1;
        public int NextIndex = -1;

        public void Reset()
        {
            EntityIdx = -1;
            PreviousIndex = -1;
            NextIndex = -1;
        }
    }
}
