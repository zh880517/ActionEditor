public unsafe class UnmanagedStructReadWrite<T> where T : struct
{
    public delegate void WriteDelegate(InternalSequence block, byte* ptr, ref T value);
    public delegate T ReadDelegate(InternalSequence block, byte* ptr);
    public static void Init(int size, WriteDelegate writeFunc, ReadDelegate readFunc)
    {
        _size = size;
        _writeFunc = writeFunc;
        _readFunc = readFunc;
    }
    private static WriteDelegate _writeFunc;
    private static ReadDelegate _readFunc;

    private static int _size;
    public static int Size => _size;
    public static void Write(InternalSequence block, byte* ptr, ref T value)
    {
        if (_writeFunc == null)
            throw new System.InvalidOperationException($"UnmanagedStructReadWrite<{typeof(T).Name}> 未初始化，请先调用 Init");
        _writeFunc(block, ptr, ref value);
    }

    public static T Read(InternalSequence block, byte* ptr)
    {
        if (_readFunc == null)
            throw new System.InvalidOperationException($"UnmanagedStructReadWrite<{typeof(T).Name}> 未初始化，请先调用 Init");
        return _readFunc(block, ptr);
    }
    
}