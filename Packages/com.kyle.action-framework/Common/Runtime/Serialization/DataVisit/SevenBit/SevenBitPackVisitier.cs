using System;
using System.Collections.Generic;
using System.IO;

namespace DataVisit
{
    public class SevenBitPackVisitier : IVisitier
    {
        private readonly MemoryStream _memory;
        private unsafe void Write(byte* bytes, int size)
        {
            _memory.Write(new ReadOnlySpan<byte>(bytes, size));
        }

        private unsafe void Write(bool[] v)
        {
            var bytes = stackalloc byte[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                bytes[i] = v[i] ? (byte)1 : (byte)0;
            }
            Write(bytes, v.Length);
        }

        private unsafe void PackHeader(uint tag, SevenBitDataType type)
        {
            byte header = (byte)((byte)type << 4);
            var array = stackalloc byte[1];
            if(tag < 15)
            {
                header |= (byte)tag;
                array[0] = header;
                Write(array, 1);
            }
            else
            {
                header |= 0xf;
                array[0] = header;
                Write(array, 1);
                PackNumber(tag);
            }
        }
        private unsafe void PackNumber(uint val)
        {
            byte* bytes = stackalloc byte[5];
            int n = 0;
            while (val > 0x7f)
            {
                bytes[n++] = (byte)(((byte)(val) & 0x7f) | 0x80);
                val >>= 7;
            }
            bytes[n++] = (byte)val;
            Write(bytes, n);
        }
        private unsafe void PackNumber(ulong val)
        {
            byte* bytes = stackalloc byte[10];
            int n = 0;
            while (val > 0x7f)
            {
                bytes[n++] = (byte)(((byte)(val) & 0x7f) | 0x80);
                val >>= 7;
            }
            bytes[n++] = (byte)val;
            Write(bytes, n);
        }

        private unsafe void PackNumber(float val)
        {
            uint v = *(uint*)&val;
            PackNumber(v);
        }

        private unsafe void PackNumber(double val)
        {
            ulong v = *(ulong*)&val;
            PackNumber(v);
        }

        private void PackInt(uint tag, int v)
        {
            if (v >= 0)
            {
                PackHeader(tag, SevenBitDataType.Positive);
                PackNumber((uint)v);
            }
            else
            {
                PackHeader(tag, SevenBitDataType.Negative);
                PackNumber((uint)(-v));
            }
        }

        private void PackUInt(uint tag, uint v)
        {
            PackHeader(tag, SevenBitDataType.Positive);
            PackNumber(v);
        }

        public MemoryStream Memory => _memory;

        public SevenBitPackVisitier(MemoryStream memory)
        {
            _memory = memory ?? new MemoryStream();
        }

        public void Visit(uint tag, string name, bool require, ref bool value)
        {
            if (!value && !require)
                return;
            PackHeader(tag, SevenBitDataType.Positive);
            PackNumber(1);
        }

        public void Visit(uint tag, string name, bool require, ref bool[] value)
        {
            if (value == null)
            {
                if(require)
                {
                    PackHeader(tag, SevenBitDataType.String);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.String);
            PackNumber((uint)value.Length);
            Write(value);
        }

        public void Visit(uint tag, string name, bool require, ref byte value)
        {
            if(value == 0 || require)
                return;
            PackHeader(tag, SevenBitDataType.Positive);
            PackNumber(value);
        }

        public void Visit(uint tag, string name, bool require, ref byte[] value)
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.String);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.String);
            PackNumber((uint)value.Length);
            unsafe
            {
                fixed (byte* bytes = value)
                {
                    Write(bytes, value.Length);
                }
            }
        }

        public void Visit(uint tag, string name, bool require, ref sbyte value)
        {
            if(value == 0 || require)
                return;
            PackInt(tag, value);
        }

        public void Visit(uint tag, string name, bool require, ref sbyte[] value)
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.String);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.String);
            PackNumber((uint)value.Length);
            unsafe
            {
                fixed (sbyte* bytes = value)
                {
                    Write((byte*)bytes, value.Length);
                }
            }
        }

        public void Visit(uint tag, string name, bool require, ref short value)
        {
            if (value == 0 || require)
                return;
            PackInt(tag, value);
        }

        public void Visit(uint tag, string name, bool require, ref ushort value)
        {
            if (value == 0 || require)
                return;
            PackUInt(tag, value);
        }

        public void Visit(uint tag, string name, bool require, ref int value)
        {
            if (value == 0 || require)
                return;
            PackInt(tag, value);
        }

        public void Visit(uint tag, string name, bool require, ref uint value)
        {
            if (value == 0 || require)
                return;
            PackUInt(tag, value);
        }

        public void Visit(uint tag, string name, bool require, ref long value)
        {
            if (value == 0 || require)
                return;
            if (value >= 0)
            {
                PackHeader(tag, SevenBitDataType.Positive);
                PackNumber((ulong)value);
            }
            else
            {
                PackHeader(tag, SevenBitDataType.Negative);
                PackNumber((ulong)(-value));
            }
        }

        public void Visit(uint tag, string name, bool require, ref ulong value)
        {
            if (value == 0 || require)
                return;
            PackHeader(tag, SevenBitDataType.Positive);
            PackNumber(value);
        }

        public void Visit(uint tag, string name, bool require, ref float value)
        {
            if (value == 0 || require)
                return;
            PackHeader(tag, SevenBitDataType.Float);
            PackNumber(value);
        }

        public void Visit(uint tag, string name, bool require, ref double value)
        {
            if (value == 0 || require)
                return;
            PackHeader(tag, SevenBitDataType.Double);
            PackNumber(value);
        }

        public void Visit(uint tag, string name, bool require, ref string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.String);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.String);
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            PackNumber((uint)bytes.Length);
            unsafe
            {
                fixed (byte* b = bytes)
                {
                    Write(b, bytes.Length);
                }
            }
        }

        public void VisitEnum<T>(uint tag, string name, bool require, ref T value) where T : Enum
        {
            int v = Convert.ToInt32(value);
            if (v == 0 || require)
                return;
            PackInt(tag, v);
        }

        public void VisitStruct<T>(uint tag, string name, bool require, ref T value) where T : struct
        {
            var posBefore = _memory.Position;
            PackHeader(tag, SevenBitDataType.StructBegin);
            var posAfterHeader = _memory.Position;
            TypeVisit<T>.Visit(this, 0, "", false, ref value);
            if (!require && _memory.Position == posAfterHeader)
            {
                //没有内容，回退
                _memory.Position = posBefore;
                _memory.SetLength(posBefore);
                return;
            }
            PackHeader(0, SevenBitDataType.StructEnd);
        }

        public void VisitClass<T>(uint tag, string name, bool require, ref T value) where T : class, new()
        {
            if (value == null && require)
            {
                PackHeader(tag, SevenBitDataType.StructBegin);
                PackHeader(0, SevenBitDataType.StructEnd);
                return;
            }
            var posBefore = _memory.Position;
            PackHeader(tag, SevenBitDataType.StructBegin);
            var posAfterHeader = _memory.Position;
            TypeVisit<T>.Visit(this, 0, "", false, ref value);
            if (!require && _memory.Position == posAfterHeader)
            {
                //没有内容，回退
                _memory.Position = posBefore;
                _memory.SetLength(posBefore);
                return;
            }
            PackHeader(0, SevenBitDataType.StructEnd);
        }
        public void VisitDynamicClass<T>(uint tag, string name, bool require, ref T value) where T : class, new()
        {
            if (value == null && require)
            {
                PackHeader(tag, SevenBitDataType.StructBegin);
                PackHeader(0, SevenBitDataType.StructEnd);
                return;
            }
            int id = DynamicTypeVisit<T>.GetTypeId(value);
            var visitFunc = DynamicTypeVisit<T>.GetVisit(id);
            if(id == -1 || visitFunc == null)
            {
                throw new Exception($"Dynamic type value is null for tag = {tag}, name = {name}.");
            }
            var posBefore = _memory.Position;
            PackHeader(tag, SevenBitDataType.DynamicBegin);//写入动态类型开始
            var posAfterHeader = _memory.Position;

            Visit(0, string.Empty, true, ref id);//写入类型id
            PackHeader(1, SevenBitDataType.StructBegin);//写入实际类型
            visitFunc(this, 0, "", false, ref value);
            if (!require && _memory.Position == posAfterHeader)
            {
                //没有内容，回退
                _memory.Position = posBefore;
                _memory.SetLength(posBefore);
                return;
            }
            PackHeader(0, SevenBitDataType.StructEnd);//写入实际类型结束

            PackHeader(0, SevenBitDataType.StructEnd);//写入动态类型结束
        }
        public void VisitArray<T>(uint tag, string name, bool require, ref T[] value)
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.Vector);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.Vector);
            PackNumber((uint)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (TypeVisit<T>.IsCustomStruct)
                    PackHeader(0, SevenBitDataType.StructBegin);
                TypeVisit<T>.Visit(this, 0, "", true, ref value[i]);
                if (TypeVisit<T>.IsCustomStruct)
                    PackHeader(0, SevenBitDataType.StructEnd);
            }
        }

        public void VisitDynamicArray<T>(uint tag, string name, bool require, ref T[] value) where T : class, new()
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.Vector);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.Vector);
            PackNumber((uint)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == null)
                    PackHeader(0, SevenBitDataType.StructEnd);
                else
                    VisitDynamicClass(0, "", true, ref value[i]);
            }
        }

        public void VisitDictionary<TKey, TValue>(uint tag, string name, bool require, ref Dictionary<TKey, TValue> value)
        {
            if(value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.Vector);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.Vector);
            PackNumber((uint)value.Count);
            foreach (var kv in value)
            {
                var key = kv.Key;
                var val = kv.Value;
                //字典按照数组处理，key和value作为结构体成员
                PackHeader(0, SevenBitDataType.StructBegin);
                
                TypeVisit<TKey>.Visit(this, 0, "", false, ref key);

                if(TypeVisit<TValue>.IsCustomStruct)
                    PackHeader(1, SevenBitDataType.StructBegin);
                TypeVisit<TValue>.Visit(this, 1, "", false, ref val);
                if (TypeVisit<TValue>.IsCustomStruct)
                    PackHeader(0, SevenBitDataType.StructEnd);

                PackHeader(0, SevenBitDataType.StructEnd);
            }
        }

        public void VisitDynamicDictionary<TKey, TValue>(uint tag, string name, bool require, ref Dictionary<TKey, TValue> value) where TValue : class, new()
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.Vector);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.Vector);
            PackNumber((uint)value.Count);
            foreach (var kv in value)
            {
                var key = kv.Key;
                var val = kv.Value;
                //字典按照数组处理，key和value作为结构体成员
                PackHeader(0, SevenBitDataType.StructBegin);
                TypeVisit<TKey>.Visit(this, 0, "", false, ref key);
                VisitDynamicClass(1, "", false, ref val);
                PackHeader(0, SevenBitDataType.StructEnd);
            }
        }

        public void VisitHashSet<T>(uint tag, string name, bool require, ref HashSet<T> value)
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.Vector);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.Vector);
            PackNumber((uint)value.Count);
            foreach (var item in value)
            {
                var itemCopy = item;
                if (TypeVisit<T>.IsCustomStruct)
                    PackHeader(0, SevenBitDataType.StructBegin);
                TypeVisit<T>.Visit(this, 0, "", true, ref itemCopy);
                if (TypeVisit<T>.IsCustomStruct)
                    PackHeader(0, SevenBitDataType.StructEnd);
            }
        }
        public void VisitList<T>(uint tag, string name, bool require, ref List<T> value)
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.Vector);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.Vector);
            PackNumber((uint)value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                var item = value[i];
                if (TypeVisit<T>.IsCustomStruct)
                    PackHeader(0, SevenBitDataType.StructBegin);
                TypeVisit<T>.Visit(this, 0, "", true, ref item);
                if (TypeVisit<T>.IsCustomStruct)
                    PackHeader(0, SevenBitDataType.StructEnd);
            }
        }
        public void VisitDynamicList<T>(uint tag, string name, bool require, ref List<T> value) where T : class, new()
        {
            if (value == null)
            {
                if (require)
                {
                    PackHeader(tag, SevenBitDataType.Vector);
                    PackNumber(0);
                }
                return;
            }
            PackHeader(tag, SevenBitDataType.Vector);
            PackNumber((uint)value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                var item = value[i];
                VisitDynamicClass(0, "", true, ref item);
            }
        }
    }
}
