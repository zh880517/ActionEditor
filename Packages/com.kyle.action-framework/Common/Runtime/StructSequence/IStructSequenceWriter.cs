public interface IStructSequenceWriter
{
    void Push<T>(int messageId, ref T value) where T : struct;
    void PushUnmanaged<T>(int messageId, ref T value) where T : unmanaged;
}
