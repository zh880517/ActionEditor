namespace ECSLite
{
    public interface IComponent
    {
    }

    public class ComponentIdentity<T> where T : IComponent
    {
        public static int Id { get; set; } = -1;
        public static bool Unique { get; set; }
    }

    public class ComponentReset<T> where T : IComponent
    {
        public static System.Action<T> OnReset = (T) => { };
    }
}