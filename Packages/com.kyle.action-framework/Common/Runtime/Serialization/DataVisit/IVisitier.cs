using System.Collections.Generic;

namespace DataVisit
{
    public static class VisitFlag
    {
        public const uint Required = 1;//必选字段,不会因为缺省而跳过
    }

    public interface IVisitier
    {
        void Visit(uint tag, string name, uint flag, ref bool value);
        void Visit(uint tag, string name, uint flag, ref bool[] value);
        void Visit(uint tag, string name, uint flag, ref byte value);
        void Visit(uint tag, string name, uint flag, ref byte[] value);
        void Visit(uint tag, string name, uint flag, ref sbyte value);
        void Visit(uint tag, string name, uint flag, ref sbyte[] value);
        void Visit(uint tag, string name, uint flag, ref short value);
        void Visit(uint tag, string name, uint flag, ref ushort value);
        void Visit(uint tag, string name, uint flag, ref int value);
        void Visit(uint tag, string name, uint flag, ref uint value);
        void Visit(uint tag, string name, uint flag, ref long value);
        void Visit(uint tag, string name, uint flag, ref ulong value);
        void Visit(uint tag, string name, uint flag, ref float value);
        void Visit(uint tag, string name, uint flag, ref double value);
        void Visit(uint tag, string name, uint flag, ref string value);
        void VisitEnum<T>(uint tag, string name, uint flag, ref T value) where T : System.Enum;
        void VisitStruct<T>(uint tag, string name, uint flag, ref T value) where T : struct;
        void VisitClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new();
        void VisitDynamicClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new();
        void VisitArray<T>(uint tag, string name, uint flag, ref T[] value);
        void VisitDynamicArray<T>(uint tag, string name, uint flag, ref T[] value) where T : class, new();
        void VisitList<T>(uint tag, string name, uint flag, ref List<T> value);
        void VisitDynamicList<T>(uint tag, string name, uint flag, ref List<T> value) where T : class, new();
        void VisitDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value);
        void VisitDynamicDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value) where TValue : class, new();
        void VisitHashSet<T>(uint tag, string name, uint flag, ref HashSet<T> value);
    }
}
