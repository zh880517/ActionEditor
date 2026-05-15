using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class InternalSequence : IDisposable
{
    public const int DefaultCapacity = 4096;

    private IntPtr _memory;
    private int _writeOffset;
    private int _readOffset;
    private int _capacity;
    private int _messageCount;
    private List<object> _references;

    public InternalSequence next;

    public int MessageCount => _messageCount;
    public int Remaining => _capacity - _writeOffset;

    public InternalSequence() : this(DefaultCapacity) { }

    public InternalSequence(int capacity)
    {
        _capacity = capacity;
        _memory = Marshal.AllocHGlobal(capacity);
        _references = new List<object>();
        _writeOffset = 0;
        _readOffset = 0;
        _messageCount = 0;
        next = null;
    }

    public unsafe byte* TryAlloc(int size)
    {
        if (_writeOffset + size > _capacity)
            return null;

        byte* ptr = (byte*)_memory + _writeOffset;
        _writeOffset += size;
        return ptr;
    }

    public unsafe byte* AllocRead(int size)
    {
        byte* ptr = (byte*)_memory + _readOffset;
        _readOffset += size;
        return ptr;
    }

    public int WriteRef(object obj)
    {
        if (obj == null)
            return -1;

        int index = _references.Count;
        _references.Add(obj);
        return index;
    }

    public object GetRef(int index)
    {
        if (index < 0)
            return null;
        return _references[index];
    }

    public void ResetRead()
    {
        _readOffset = 0;
    }

    public void IncrementMessageCount()
    {
        _messageCount++;
    }

    public void Reset()
    {
        _writeOffset = 0;
        _readOffset = 0;
        _messageCount = 0;
        _references.Clear();
        next = null;
    }

    public void Dispose()
    {
        if (_memory != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_memory);
            _memory = IntPtr.Zero;
        }
        _references = null;
        next = null;
    }
}
