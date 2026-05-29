namespace ECSLite
{
    public interface IStaticComponent
    {
    }

    public class StaticComponentIdentity<T> where T : IStaticComponent
    {
        public static int Id { get; set; } = -1;
    }
}