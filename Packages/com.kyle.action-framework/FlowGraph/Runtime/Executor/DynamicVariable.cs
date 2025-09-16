using System.Collections;
using System.Collections.Generic;

namespace Flow
{
    public class DynamicVariable
    {
        public virtual void Recyle() { }

        private static readonly List<IList> pools = new List<IList>();
        internal static void OnPoolCreate(IList list)
        {
            pools.Add(list);
        }
        public static void ClearAllPool()
        {
            foreach (var item in pools)
            {
                item.Clear();
            }
        }
    }

    public class TDynamicVariable<T> : DynamicVariable
    {
        public T Value;
        private static List<TDynamicVariable<T>> pool;

        public override void Recyle()
        {
            pool?.Add(this);
        }

        public static TDynamicVariable<T> Get()
        {
            if (pool == null)
            {
                pool = new List<TDynamicVariable<T>>();
                OnPoolCreate(pool);
            }
            if (pool.Count > 0)
            {
                var item = pool[^1];
                pool.RemoveAt(pool.Count - 1);
                return item;
            }
            return new TDynamicVariable<T>();
        }
    }
}
