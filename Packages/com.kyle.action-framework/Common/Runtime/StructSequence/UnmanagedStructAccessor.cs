public unsafe class UnmanagedStructAccessor<T> where T : unmanaged
{
    public static int Size => sizeof(T);

    public static T Read(InternalSequence block, byte* ptr)
    {
        return *(T*)ptr;
    }

    public static void Write(InternalSequence block, byte* ptr, ref T value)
    {
        *(T*)ptr = value;
    }
}