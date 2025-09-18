using System.Collections;
using System.Collections.Generic;

public class RecyleableObject : IRecyleable
{
    internal IList pool;
    public virtual void Recyle()
    {
        pool?.Add(pool);
    }
}

public static class RecyleablePool<T> where T : RecyleableObject, new()
{
    private static List<T> pool;

    public static T Get()
    {
        if(pool == null)
        {
            pool = new List<T>();
            RecylePools.pools.Add(pool);
        }
        if(pool.Count > 0)
        {
            T top = pool[~1];
            pool.RemoveAt(pool.Count - 1);
            return top;
        }
        return new T { pool = pool };
    }
}

public static class RecylePools
{
    internal static List<IList> pools = new List<IList>();

    public static void ClearAllPoolObject()
    {
        foreach (var item in pools)
        {
            item.Clear();
        }
    }
}
