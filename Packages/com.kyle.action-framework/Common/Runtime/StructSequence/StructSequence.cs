using System;
using System.Collections.Generic;

public class StructSequence : IStructSequenceWriter, IStructSequenceReader, IDisposable
{
    private InternalSequence _head;
    private InternalSequence _current;
    private List<SequenceMeta> _metas;
    private InternalSequencePool _pool;

    public IReadOnlyList<SequenceMeta> Metas => _metas;

    public StructSequence()
    {
        _pool = new InternalSequencePool();
        _metas = new List<SequenceMeta>();
        _head = _pool.Rent();
        _current = _head;
    }

    public void Push<T>(int messageId, ref T value) where T : struct
    {
        int payloadSize = UnsafeStructAccessor<T>.Size;
        if (_current.Remaining < payloadSize)
        {
            var newBlock = _pool.Rent();
            _current.next = newBlock;
            _current = newBlock;
        }
        int offset = _current.WriteOffset;
        System.IntPtr ptr = _current.TryAlloc(payloadSize);
        UnsafeStructAccessor<T>.Write(_current, ptr, ref value);
        _metas.Add(new SequenceMeta { MessageID = messageId, Block = _current, Offset = offset });
    }

    public void PushUnmanaged<T>(int messageId, ref T value) where T : unmanaged
    {
        int payloadSize = UnmanagedStructAccessor<T>.Size;
        if (_current.Remaining < payloadSize)
        {
            var newBlock = _pool.Rent();
            _current.next = newBlock;
            _current = newBlock;
        }
        int offset = _current.WriteOffset;
        System.IntPtr ptr = _current.TryAlloc(payloadSize);
        UnmanagedStructAccessor<T>.Write(_current, ptr, ref value);
        _metas.Add(new SequenceMeta { MessageID = messageId, Block = _current, Offset = offset });
    }

    public T Read<T>(SequenceMeta meta) where T : struct
    {
        System.IntPtr ptr = meta.Block.GetPayloadPtr(meta.Offset);
        return UnsafeStructAccessor<T>.Read(meta.Block, ptr);
    }

    public T ReadUnmanaged<T>(SequenceMeta meta) where T : unmanaged
    {
        System.IntPtr ptr = meta.Block.GetPayloadPtr(meta.Offset);
        return UnmanagedStructAccessor<T>.Read(meta.Block, ptr);
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
        _metas.Clear();
        _head = _pool.Rent();
        _current = _head;
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
        _metas = null;
    }
}
