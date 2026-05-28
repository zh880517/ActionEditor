using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class InternalSequence : IDisposable
{
    public const int DefaultCapacity = 4096;

    private IntPtr _memory;
    private int _writeOffset;
    private int _capacity;
    private List<object> _references;

    public InternalSequence next;

    public int Capacity => _capacity;
    public int Remaining => _capacity - _writeOffset;
    public int WriteOffset => _writeOffset;

    public InternalSequence() : this(DefaultCapacity) { }

    public InternalSequence(int capacity)
    {
        _capacity = capacity;
        _memory = Marshal.AllocHGlobal(capacity);
        _references = new List<object>();
        _writeOffset = 0;
        next = null;
    }

    public IntPtr TryAlloc(int size)
    {
        if (_writeOffset + size > _capacity)
            return IntPtr.Zero;

        IntPtr ptr = IntPtr.Add(_memory, _writeOffset);
        _writeOffset += size;
        return ptr;
    }

    public IntPtr GetPayloadPtr(int offset)
    {
        return IntPtr.Add(_memory, offset);
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

    public void Reset()
    {
        _writeOffset = 0;
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
