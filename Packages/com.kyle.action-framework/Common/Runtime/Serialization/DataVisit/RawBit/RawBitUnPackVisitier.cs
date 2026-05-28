using System;
using System.Collections.Generic;
using System.Text;

namespace DataVisit
{
    public class RawBitUnPackVisitier : IVisitier
    {
        private ArraySegment<byte> _data;
        private int _pos;

        public RawBitUnPackVisitier(byte[] data)
            : this(new ArraySegment<byte>(data))
        {
        }

        public RawBitUnPackVisitier(ArraySegment<byte> data)
        {
            Reset(data);
        }

        public void Reset(byte[] data)
        {
            Reset(new ArraySegment<byte>(data));
        }

        public void Reset(ArraySegment<byte> data)
        {
            _data = data;
            _pos = 0;
        }

        private void EnsureSize(int size)
        {
            if (_pos + size > _data.Count)
            {
                throw new Exception("RawBitUnPack end of data");
            }
        }

        private byte ReadByte()
        {
            EnsureSize(1);
            return _data.Array[_data.Offset + _pos++];
        }

        private void ReadBytes(byte[] buffer, int offset, int count)
        {
            EnsureSize(count);
            Array.Copy(_data.Array, _data.Offset + _pos, buffer, offset, count);
            _pos += count;
        }

        private unsafe void ReadRaw(void* buffer, int count)
        {
            EnsureSize(count);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                Buffer.MemoryCopy(ptr, buffer, count, count);
            }
            _pos += count;
        }

        private unsafe short ReadInt16()
        {
            EnsureSize(2);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                short value = *(short*)ptr;
                _pos += 2;
                return value;
            }
        }

        private unsafe ushort ReadUInt16()
        {
            EnsureSize(2);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                ushort value = *(ushort*)ptr;
                _pos += 2;
                return value;
            }
        }

        private unsafe int ReadInt32()
        {
            EnsureSize(4);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                int value = *(int*)ptr;
                _pos += 4;
                return value;
            }
        }

        private unsafe uint ReadUInt32()
        {
            EnsureSize(4);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                uint value = *(uint*)ptr;
                _pos += 4;
                return value;
            }
        }

        private unsafe long ReadInt64()
        {
            EnsureSize(8);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                long value = *(long*)ptr;
                _pos += 8;
                return value;
            }
        }

        private unsafe ulong ReadUInt64()
        {
            EnsureSize(8);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                ulong value = *(ulong*)ptr;
                _pos += 8;
                return value;
            }
        }

        private unsafe float ReadSingle()
        {
            EnsureSize(4);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                float value = *(float*)ptr;
                _pos += 4;
                return value;
            }
        }

        private unsafe double ReadDouble()
        {
            EnsureSize(8);
            fixed (byte* ptr = &_data.Array[_data.Offset + _pos])
            {
                double value = *(double*)ptr;
                _pos += 8;
                return value;
            }
        }

        private string ReadString()
        {
            int length = ReadInt32();
            if (length < 0)
                return null;
            if (length == 0)
                return string.Empty;
            EnsureSize(length);
            string value = Encoding.UTF8.GetString(_data.Array, _data.Offset + _pos, length);
            _pos += length;
            return value;
        }

        private void ReadBytesWithLength(ref byte[] value)
        {
            int length = ReadInt32();
            if (length < 0)
            {
                value = null;
                return;
            }
            if (length == 0)
            {
                value = Array.Empty<byte>();
                return;
            }
            if (value == null || value.Length != length)
            {
                value = new byte[length];
            }
            ReadBytes(value, 0, length);
        }

        private unsafe void ReadSBytesWithLength(ref sbyte[] value)
        {
            int length = ReadInt32();
            if (length < 0)
            {
                value = null;
                return;
            }
            if (length == 0)
            {
                value = Array.Empty<sbyte>();
                return;
            }
            if (value == null || value.Length != length)
            {
                value = new sbyte[length];
            }
            fixed (sbyte* ptr = value)
            {
                ReadRaw(ptr, length);
            }
        }

        private void ReadBoolsWithLength(ref bool[] value)
        {
            int length = ReadInt32();
            if (length < 0)
            {
                value = null;
                return;
            }
            if (length == 0)
            {
                value = Array.Empty<bool>();
                return;
            }
            if (value == null || value.Length != length)
            {
                value = new bool[length];
            }
            EnsureSize(length);
            for (int i = 0; i < length; i++)
            {
                value[i] = _data.Array[_data.Offset + _pos + i] != 0;
            }
            _pos += length;
        }

        public void Visit(uint tag, string name, uint flag, ref bool value)
        {
            value = ReadByte() != 0;
        }

        public void Visit(uint tag, string name, uint flag, ref bool[] value)
        {
            ReadBoolsWithLength(ref value);
        }

        public void Visit(uint tag, string name, uint flag, ref byte value)
        {
            value = ReadByte();
        }

        public void Visit(uint tag, string name, uint flag, ref byte[] value)
        {
            ReadBytesWithLength(ref value);
        }

        public void Visit(uint tag, string name, uint flag, ref sbyte value)
        {
            value = (sbyte)ReadByte();
        }

        public void Visit(uint tag, string name, uint flag, ref sbyte[] value)
        {
            ReadSBytesWithLength(ref value);
        }

        public void Visit(uint tag, string name, uint flag, ref short value)
        {
            value = ReadInt16();
        }

        public void Visit(uint tag, string name, uint flag, ref ushort value)
        {
            value = ReadUInt16();
        }

        public void Visit(uint tag, string name, uint flag, ref int value)
        {
            value = ReadInt32();
        }

        public void Visit(uint tag, string name, uint flag, ref uint value)
        {
            value = ReadUInt32();
        }

        public void Visit(uint tag, string name, uint flag, ref long value)
        {
            value = ReadInt64();
        }

        public void Visit(uint tag, string name, uint flag, ref ulong value)
        {
            value = ReadUInt64();
        }

        public void Visit(uint tag, string name, uint flag, ref float value)
        {
            value = ReadSingle();
        }

        public void Visit(uint tag, string name, uint flag, ref double value)
        {
            value = ReadDouble();
        }

        public void Visit(uint tag, string name, uint flag, ref string value)
        {
            value = ReadString();
        }

        public void VisitEnum<T>(uint tag, string name, uint flag, ref T value) where T : Enum
        {
            int raw = ReadInt32();
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            object rawValue;
            if (underlyingType == typeof(byte))
                rawValue = unchecked((byte)raw);
            else if (underlyingType == typeof(sbyte))
                rawValue = unchecked((sbyte)raw);
            else if (underlyingType == typeof(short))
                rawValue = unchecked((short)raw);
            else if (underlyingType == typeof(ushort))
                rawValue = unchecked((ushort)raw);
            else if (underlyingType == typeof(int))
                rawValue = raw;
            else if (underlyingType == typeof(uint))
                rawValue = unchecked((uint)raw);
            else if (underlyingType == typeof(long))
                rawValue = (long)raw;
            else if (underlyingType == typeof(ulong))
                rawValue = (ulong)unchecked((uint)raw);
            else
                throw new NotSupportedException($"Unsupported enum underlying type: {underlyingType}");
            value = (T)Enum.ToObject(typeof(T), rawValue);
        }

        public void VisitStruct<T>(uint tag, string name, uint flag, ref T value) where T : struct
        {
            TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value);
        }

        public void VisitClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            var marker = (RawBitDataType)ReadByte();
            if (marker == RawBitDataType.Null)
            {
                value = null;
                return;
            }
            if (marker != RawBitDataType.Static)
                throw new Exception($"RawBitUnPack invalid class marker: {marker}");
            value ??= new T();
            TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value);
        }

        public void VisitDynamicClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            var marker = (RawBitDataType)ReadByte();
            if (marker == RawBitDataType.Null)
            {
                value = null;
                return;
            }
            if (marker == RawBitDataType.Static)
            {
                value ??= new T();
                TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref value);
                return;
            }
            if (marker != RawBitDataType.Dynamic)
                throw new Exception($"RawBitUnPack invalid dynamic marker: {marker}");
            int typeId = ReadInt32();
            var visitier = TypeVisit.GetVisit(typeId);
            object obj = value;
            bool canReuse = false;
            if (obj != null)
            {
                try
                {
                    canReuse = TypeVisit.GetTypeId(obj) == typeId;
                }
                catch
                {
                    // 旧对象可能来自未注册类型，不能复用。
                    canReuse = false;
                }
            }
            if (!canReuse)
            {
                obj = visitier.New();
            }
            visitier.Visit(this, 0, string.Empty, 0, ref obj);
            value = (T)obj;
        }

        public void VisitArray<T>(uint tag, string name, uint flag, ref T[] value)
        {
            int length = ReadInt32();
            if (length < 0)
            {
                value = null;
                return;
            }
            if (length == 0)
            {
                value = Array.Empty<T>();
                return;
            }
            bool reuseItems = value != null && value.Length == length;
            if (!reuseItems)
            {
                value = new T[length];
            }
            for (int i = 0; i < length; i++)
            {
                T item = reuseItems ? value[i] : TypeVisitT<T>.New();
                TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref item);
                value[i] = item;
            }
        }

        public void VisitDynamicArray<T>(uint tag, string name, uint flag, ref T[] value) where T : class, new()
        {
            int length = ReadInt32();
            if (length < 0)
            {
                value = null;
                return;
            }
            if (length == 0)
            {
                value = Array.Empty<T>();
                return;
            }
            if (value == null || value.Length != length)
            {
                value = new T[length];
            }
            for (int i = 0; i < length; i++)
            {
                T item = value[i];
                VisitDynamicClass(0, string.Empty, 0, ref item);
                value[i] = item;
            }
        }

        public void VisitList<T>(uint tag, string name, uint flag, ref List<T> value)
        {
            int count = ReadInt32();
            if (count < 0)
            {
                value = null;
                return;
            }
            value ??= new List<T>(count);
            if (value.Capacity < count)
            {
                value.Capacity = count;
            }
            if (count == 0)
            {
                value.Clear();
                return;
            }
            int oldCount = value.Count;
            for (int i = 0; i < count; i++)
            {
                T item = i < oldCount ? value[i] : TypeVisitT<T>.New();
                TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref item);
                if (i < oldCount)
                    value[i] = item;
                else
                    value.Add(item);
            }
            if (value.Count > count)
            {
                value.RemoveRange(count, value.Count - count);
            }
        }

        public void VisitDynamicList<T>(uint tag, string name, uint flag, ref List<T> value) where T : class, new()
        {
            int count = ReadInt32();
            if (count < 0)
            {
                value = null;
                return;
            }
            value ??= new List<T>(count);
            if (value.Capacity < count)
            {
                value.Capacity = count;
            }
            if (count == 0)
            {
                value.Clear();
                return;
            }
            int oldCount = value.Count;
            for (int i = 0; i < count; i++)
            {
                T item = i < oldCount ? value[i] : default;
                VisitDynamicClass(0, string.Empty, 0, ref item);
                if (i < oldCount)
                    value[i] = item;
                else
                    value.Add(item);
            }
            if (value.Count > count)
            {
                value.RemoveRange(count, value.Count - count);
            }
        }

        public void VisitDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value)
        {
            int count = ReadInt32();
            if (count < 0)
            {
                value = null;
                return;
            }
            value ??= new Dictionary<TKey, TValue>(count);
            value.Clear();
            if (count == 0)
                return;
            for (int i = 0; i < count; i++)
            {
                TKey key = TypeVisitT<TKey>.New();
                TValue val = TypeVisitT<TValue>.New();
                TypeVisitT<TKey>.Visit(this, 0, string.Empty, 0, ref key);
                TypeVisitT<TValue>.Visit(this, 1, string.Empty, 0, ref val);
                value.Add(key, val);
            }
        }

        public void VisitDynamicDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value)
            where TValue : class, new()
        {
            int count = ReadInt32();
            if (count < 0)
            {
                value = null;
                return;
            }
            value ??= new Dictionary<TKey, TValue>(count);
            value.Clear();
            if (count == 0)
                return;
            for (int i = 0; i < count; i++)
            {
                TKey key = TypeVisitT<TKey>.New();
                TValue val = default;
                TypeVisitT<TKey>.Visit(this, 0, string.Empty, 0, ref key);
                VisitDynamicClass(1, string.Empty, 0, ref val);
                value.Add(key, val);
            }
        }

        public void VisitHashSet<T>(uint tag, string name, uint flag, ref HashSet<T> value)
        {
            int count = ReadInt32();
            if (count < 0)
            {
                value = null;
                return;
            }
            value ??= new HashSet<T>();
            value.Clear();
            if (count == 0)
                return;
            for (int i = 0; i < count; i++)
            {
                T item = TypeVisitT<T>.New();
                TypeVisitT<T>.Visit(this, 0, string.Empty, 0, ref item);
                value.Add(item);
            }
        }
    }
}
