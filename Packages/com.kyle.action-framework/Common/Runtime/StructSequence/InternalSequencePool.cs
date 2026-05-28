using System;
using System.Collections.Generic;

internal class InternalSequencePool : IDisposable
{
    private readonly Stack<InternalSequence> _pool = new Stack<InternalSequence>();
    private readonly int _blockCapacity;

    public InternalSequencePool(int blockCapacity = InternalSequence.DefaultCapacity)
    {
        _blockCapacity = blockCapacity;
    }

    public InternalSequence Rent()
    {
        return Rent(_blockCapacity);
    }

    public InternalSequence Rent(int minCapacity)
    {
        while (_pool.Count > 0)
        {
            var block = _pool.Pop();
            if (block.Capacity >= minCapacity)
                return block;
            block.Dispose();
        }

        return new InternalSequence(Math.Max(_blockCapacity, minCapacity));
    }

    public void Return(InternalSequence block)
    {
        if (block == null)
            return;
        block.Reset();
        _pool.Push(block);
    }

    public void Dispose()
    {
        while (_pool.Count > 0)
        {
            _pool.Pop().Dispose();
        }
    }
}
