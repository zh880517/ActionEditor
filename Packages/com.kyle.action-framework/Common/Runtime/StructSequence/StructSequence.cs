using System;

public class StructSequence : IDisposable
{
    private InternalSequence _head;
    private InternalSequence _current;
    private Action<InternalSequence>[] _handlers;
    private int _totalMessageCount;
    private InternalSequencePool _pool;

    public int TotalMessageCount => _totalMessageCount;

    public void Init(int maxTypeCount = 64)
    {
        _pool = new InternalSequencePool();
        _handlers = new Action<InternalSequence>[maxTypeCount];
        _head = _pool.Rent();
        _current = _head;
        _totalMessageCount = 0;
    }

    public void RegisterHandler(int typeIndex, Action<InternalSequence> handler)
    {
        _handlers[typeIndex] = handler;
    }

    public InternalSequence AllocMessage(int messageSize)
    {
        if (_current.Remaining < messageSize)
        {
            var newBlock = _pool.Rent();
            _current.next = newBlock;
            _current = newBlock;
        }
        _totalMessageCount++;
        return _current;
    }

    public unsafe void Consume()
    {
        var block = _head;
        while (block != null)
        {
            block.ResetRead();
            for (int i = 0; i < block.MessageCount; i++)
            {
                byte* hdr = block.AllocRead(4);
                int typeIndex = *(int*)hdr;
                _handlers[typeIndex](block);
            }
            block = block.next;
        }
    }

    public void Reset()
    {
        var block = _head;
        while (block != null)
        {
            var next = block.next;
            _pool.Return(block);
            block = next;
        }
        _head = _pool.Rent();
        _current = _head;
        _totalMessageCount = 0;
    }

    public void Dispose()
    {
        var block = _head;
        while (block != null)
        {
            var next = block.next;
            block.Dispose();
            block = next;
        }
        _head = null;
        _current = null;
        _pool?.Dispose();
        _pool = null;
    }
}
