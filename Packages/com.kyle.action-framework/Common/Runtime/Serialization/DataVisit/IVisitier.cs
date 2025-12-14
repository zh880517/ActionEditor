using System.Collections.Generic;

namespace DataVisit
{
    public interface IVisitier
    {
        void Visit(uint tag, string name, bool require, ref bool value);
        void Visit(uint tag, string name, bool require, ref bool[] value);
        void Visit(uint tag, string name, bool require, ref byte value);
        void Visit(uint tag, string name, bool require, ref byte[] value);
        void Visit(uint tag, string name, bool require, ref sbyte value);
        void Visit(uint tag, string name, bool require, ref sbyte[] value);
        void Visit(uint tag, string name, bool require, ref short value);
        void Visit(uint tag, string name, bool require, ref ushort value);
        void Visit(uint tag, string name, bool require, ref int value);
        void Visit(uint tag, string name, bool require, ref uint value);
        void Visit(uint tag, string name, bool require, ref long value);
        void Visit(uint tag, string name, bool require, ref ulong value);
        void Visit(uint tag, string name, bool require, ref float value);
        void Visit(uint tag, string name, bool require, ref double value);
        void Visit(uint tag, string name, bool require, ref string value);
        void VisitEnum<T>(uint tag, string name, bool require, ref T value) where T : System.Enum;
        void VisitStruct<T>(uint tag, string name, bool require, ref T value) where T : struct;
        void VisitClass<T>(uint tag, string name, bool require, ref T value) where T : class, new();
        void VisitDynamicClass<T>(uint tag, string name, bool require, ref T value) where T : class, new();
        void VisitArray<T>(uint tag, string name, bool require, ref T[] value);
        void VisitDynamicArray<T>(uint tag, string name, bool require, ref T[] value) where T : class, new();
        void VisitList<T>(uint tag, string name, bool require, ref List<T> value);
        void VisitDynamicList<T>(uint tag, string name, bool require, ref List<T> value) where T : class, new();
        void VisitDictionary<TKey, TValue>(uint tag, string name, bool require, ref Dictionary<TKey, TValue> value);
        void VisitDynamicDictionary<TKey, TValue>(uint tag, string name, bool require, ref Dictionary<TKey, TValue> value) where TValue : class, new();
        void VisitHashSet<T>(uint tag, string name, bool require, ref HashSet<T> value);
    }
}
