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
        if (_pool.Count > 0)
            return _pool.Pop();

        return new InternalSequence(_blockCapacity);
    }

    public void Return(InternalSequence block)
    {
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
