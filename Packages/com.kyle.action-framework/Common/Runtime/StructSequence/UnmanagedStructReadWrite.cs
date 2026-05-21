public unsafe class UnmanagedStructReadWrite<T> where T : struct, IUnmanagedStruct
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

    private static int _size = System.Runtime.InteropServices.Marshal.SizeOf<T>();
    public static int Size => _size;
    public static void Write(InternalSequence block, byte* ptr, ref T value)
    {
        if(_writeFunc != null)
        {
            _writeFunc(block, ptr, ref value);
            return;
        }
        System.Runtime.InteropServices.Marshal.StructureToPtr(value, (System.IntPtr)ptr, false);
    }

    public static T Read(InternalSequence block, byte* ptr)
    {
        if(_readFunc != null)
            return _readFunc(block, ptr);
        return System.Runtime.InteropServices.Marshal.PtrToStructure<T>((System.IntPtr)ptr);
    }
    
}