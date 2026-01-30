using System.Collections.Generic;

namespace DataVisit
{
    public sealed class ResetVisitier : IVisitier
    {
        public readonly static ResetVisitier Default = new ResetVisitier();

        public void Visit(uint tag, string name, uint flag, ref bool value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref bool[] value) => value = EmptyArray<bool>.Array;
        public void Visit(uint tag, string name, uint flag, ref byte value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref byte[] value) => value = EmptyArray<byte>.Array;
        public void Visit(uint tag, string name, uint flag, ref sbyte value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref sbyte[] value) => value = EmptyArray<sbyte>.Array;
        public void Visit(uint tag, string name, uint flag, ref short value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref ushort value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref int value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref uint value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref long value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref ulong value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref float value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref double value) => value = default;
        public void Visit(uint tag, string name, uint flag, ref string value) => value = string.Empty;

        public void VisitEnum<T>(uint tag, string name, uint flag, ref T value) where T : System.Enum
            => value = default;

        public void VisitStruct<T>(uint tag, string name, uint flag, ref T value) where T : struct
            => TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value);

        public void VisitClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            if (value == null)
                return;
            TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value);
        }

        public void VisitDynamicClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            if (value == null)
                return;
            int id = TypeVisit.GetTypeId(value);
            var visitier = TypeVisit.GetVisit(id);
            object obj = value;
            visitier.Visit(this, 0, string.Empty, 0, ref obj);
            value = (T)obj;
        }

        public void VisitArray<T>(uint tag, string name, uint flag, ref T[] value)
            => value = EmptyArray<T>.Array;

        public void VisitDynamicArray<T>(uint tag, string name, uint flag, ref T[] value) where T : class, new()
            => value = EmptyArray<T>.Array;

        public void VisitList<T>(uint tag, string name, uint flag, ref List<T> value)
            => value?.Clear();

        public void VisitDynamicList<T>(uint tag, string name, uint flag, ref List<T> value) where T : class, new()
            => value?.Clear();

        public void VisitDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value)
            => value?.Clear();

        public void VisitDynamicDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value)
            where TValue : class, new()
            => value?.Clear();

        public void VisitHashSet<T>(uint tag, string name, uint flag, ref HashSet<T> value)
            => value?.Clear();

    }
}
