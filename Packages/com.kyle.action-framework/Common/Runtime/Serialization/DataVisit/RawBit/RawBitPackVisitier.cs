using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataVisit
{
    public class RawBitPackVisitier : IVisitier
    {
        private const int StackallocByteLimit = 256;
        private readonly MemoryStream _memory;

        public RawBitPackVisitier(MemoryStream memory)
        {
            _memory = memory ?? new MemoryStream();
        }

        public MemoryStream Memory => _memory;

        private void WriteByte(byte value)
        {
            _memory.WriteByte(value);
        }

        private void WriteBytes(byte[] value)
        {
            _memory.Write(value, 0, value.Length);
        }

        private void WriteBytes(byte[] value, int offset, int count)
        {
            _memory.Write(value, offset, count);
        }

        private unsafe void WriteRaw(void* bytes, int size)
        {
            _memory.Write(new ReadOnlySpan<byte>(bytes, size));
        }

        private unsafe void WriteInt16(short value) => WriteRaw(&value, sizeof(short));
        private unsafe void WriteUInt16(ushort value) => WriteRaw(&value, sizeof(ushort));
        private unsafe void WriteInt32(int value) => WriteRaw(&value, sizeof(int));
        private unsafe void WriteUInt32(uint value) => WriteRaw(&value, sizeof(uint));
        private unsafe void WriteInt64(long value) => WriteRaw(&value, sizeof(long));
        private unsafe void WriteUInt64(ulong value) => WriteRaw(&value, sizeof(ulong));
        private unsafe void WriteSingle(float value) => WriteRaw(&value, sizeof(float));
        private unsafe void WriteDouble(double value) => WriteRaw(&value, sizeof(double));

        private unsafe void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            int byteCount = Encoding.UTF8.GetByteCount(value);
            WriteInt32(byteCount);
            if (byteCount == 0)
                return;

            if (byteCount <= StackallocByteLimit)
            {
                byte* bytes = stackalloc byte[byteCount];
                fixed (char* chars = value)
                {
                    int written = Encoding.UTF8.GetBytes(chars, value.Length, bytes, byteCount);
                    WriteRaw(bytes, written);
                }
                return;
            }

            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                int written = Encoding.UTF8.GetBytes(value, 0, value.Length, rented, 0);
                WriteBytes(rented, 0, written);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        private void WriteBytesWithLength(byte[] value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            if (value.Length == 0)
            {
                WriteInt32(0);
                return;
            }
            WriteInt32(value.Length);
            WriteBytes(value);
        }

        private unsafe void WriteSBytesWithLength(sbyte[] value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            if (value.Length == 0)
            {
                WriteInt32(0);
                return;
            }
            WriteInt32(value.Length);
            fixed (sbyte* ptr = value)
            {
                WriteRaw(ptr, value.Length);
            }
        }

        private unsafe void WriteBoolsWithLength(bool[] value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            if (value.Length == 0)
            {
                WriteInt32(0);
                return;
            }
            WriteInt32(value.Length);

            if (value.Length <= StackallocByteLimit)
            {
                byte* bytes = stackalloc byte[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    bytes[i] = value[i] ? (byte)1 : (byte)0;
                }
                WriteRaw(bytes, value.Length);
                return;
            }

            var rented = ArrayPool<byte>.Shared.Rent(value.Length);
            try
            {
                for (int i = 0; i < value.Length; i++)
                {
                    rented[i] = value[i] ? (byte)1 : (byte)0;
                }
                WriteBytes(rented, 0, value.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public void Visit(uint tag, string name, uint flag, ref bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        public void Visit(uint tag, string name, uint flag, ref bool[] value)
        {
            WriteBoolsWithLength(value);
        }

        public void Visit(uint tag, string name, uint flag, ref byte value)
        {
            WriteByte(value);
        }

        public void Visit(uint tag, string name, uint flag, ref byte[] value)
        {
            WriteBytesWithLength(value);
        }

        public void Visit(uint tag, string name, uint flag, ref sbyte value)
        {
            WriteByte((byte)value);
        }

        public void Visit(uint tag, string name, uint flag, ref sbyte[] value)
        {
            WriteSBytesWithLength(value);
        }

        public void Visit(uint tag, string name, uint flag, ref short value)
        {
            WriteInt16(value);
        }

        public void Visit(uint tag, string name, uint flag, ref ushort value)
        {
            WriteUInt16(value);
        }

        public void Visit(uint tag, string name, uint flag, ref int value)
        {
            WriteInt32(value);
        }

        public void Visit(uint tag, string name, uint flag, ref uint value)
        {
            WriteUInt32(value);
        }

        public void Visit(uint tag, string name, uint flag, ref long value)
        {
            WriteInt64(value);
        }

        public void Visit(uint tag, string name, uint flag, ref ulong value)
        {
            WriteUInt64(value);
        }

        public void Visit(uint tag, string name, uint flag, ref float value)
        {
            WriteSingle(value);
        }

        public void Visit(uint tag, string name, uint flag, ref double value)
        {
            WriteDouble(value);
        }

        public void Visit(uint tag, string name, uint flag, ref string value)
        {
            WriteString(value);
        }

        public void VisitEnum<T>(uint tag, string name, uint flag, ref T value) where T : Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            if (underlyingType == typeof(uint))
            {
                WriteInt32(unchecked((int)Convert.ToUInt32(value)));
                return;
            }
            if (underlyingType == typeof(ulong))
            {
                ulong raw = Convert.ToUInt64(value);
                if (raw > uint.MaxValue)
                    throw new OverflowException($"RawBit enum {typeof(T).FullName} value {raw} cannot fit in the legacy 32-bit enum format.");
                WriteInt32(unchecked((int)(uint)raw));
                return;
            }
            long signed = Convert.ToInt64(value);
            if (signed < int.MinValue || signed > int.MaxValue)
                throw new OverflowException($"RawBit enum {typeof(T).FullName} value {signed} cannot fit in the legacy 32-bit enum format.");
            WriteInt32((int)signed);
        }

        public void VisitStruct<T>(uint tag, string name, uint flag, ref T value) where T : struct
        {
            TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value);
        }

        public void VisitClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            if (value == null)
            {
                WriteByte((byte)RawBitDataType.Null);
                return;
            }
            WriteByte((byte)RawBitDataType.Static);
            TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value);
        }

        public void VisitDynamicClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            if (value == null)
            {
                WriteByte((byte)RawBitDataType.Null);
                return;
            }
            WriteByte((byte)RawBitDataType.Dynamic);
            int id = TypeVisit.GetTypeId(value);
            WriteInt32(id);
            var visitier = TypeVisit.GetVisit(id);
            object obj = value;
            visitier.Visit(this, 0, string.Empty, 0, ref obj);
            value = (T)obj;
        }

        public void VisitArray<T>(uint tag, string name, uint flag, ref T[] value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            int length = value.Length;
            WriteInt32(length);
            if (length == 0)
                return;
            for (int i = 0; i < value.Length; i++)
            {
                TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value[i]);
            }
        }

        public void VisitDynamicArray<T>(uint tag, string name, uint flag, ref T[] value) where T : class, new()
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            int length = value.Length;
            WriteInt32(length);
            if (length == 0)
                return;
            for (int i = 0; i < value.Length; i++)
            {
                VisitDynamicClass(0, string.Empty, 0, ref value[i]);
            }
        }

        public void VisitList<T>(uint tag, string name, uint flag, ref List<T> value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            int count = value.Count;
            WriteInt32(count);
            if (count == 0)
                return;
            for (int i = 0; i < count; i++)
            {
                var item = value[i];
                TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref item);
            }
        }

        public void VisitDynamicList<T>(uint tag, string name, uint flag, ref List<T> value) where T : class, new()
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            int count = value.Count;
            WriteInt32(count);
            if (count == 0)
                return;
            for (int i = 0; i < count; i++)
            {
                var item = value[i];
                VisitDynamicClass(0, string.Empty, 0, ref item);
            }
        }

        public void VisitDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            int count = value.Count;
            WriteInt32(count);
            if (count == 0)
                return;
            foreach (var kv in value)
            {
                var key = kv.Key;
                var val = kv.Value;
                TypeVisitT<TKey>.Visit(this, 0, string.Empty, 0, ref key);
                TypeVisitT<TValue>.Visit(this, 1, string.Empty, 0, ref val);
            }
        }

        public void VisitDynamicDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value)
            where TValue : class, new()
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            int count = value.Count;
            WriteInt32(count);
            if (count == 0)
                return;
            foreach (var kv in value)
            {
                var key = kv.Key;
                var val = kv.Value;
                TypeVisitT<TKey>.Visit(this, 0, string.Empty, 0, ref key);
                VisitDynamicClass(1, string.Empty, 0, ref val);
            }
        }

        public void VisitHashSet<T>(uint tag, string name, uint flag, ref HashSet<T> value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            int count = value.Count;
            WriteInt32(count);
            if (count == 0)
                return;
            foreach (var itemValue in value)
            {
                var item = itemValue;
                TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref item);
            }
        }
    }
}
