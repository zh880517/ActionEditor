namespace ECSLite
{
    public interface ICleanupSystem : ISystem
    {
        void OnCleanup();
    }
}
