using System;
using System.Collections.Generic;
using System.Text;

public interface IPoolable
{
    void Reset();
}

public class Pool<T> where T : IPoolable, new()
{
    private readonly Stack<T> pool = new Stack<T>();
    public T Get()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }
        return new T();
    }
    public void Release(T item)
    {
        item.Reset();
        pool.Push(item);
    }
}
