using System.Collections.Generic;

public interface IStructSequenceReader
{
    IReadOnlyList<SequenceMeta> Metas { get; }
    T Read<T>(SequenceMeta meta) where T : struct;
    T ReadUnmanaged<T>(SequenceMeta meta) where T : unmanaged;
}
