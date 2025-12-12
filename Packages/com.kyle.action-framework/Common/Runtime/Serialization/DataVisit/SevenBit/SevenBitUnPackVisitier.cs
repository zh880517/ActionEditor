using System;
using System.Collections.Generic;

namespace DataVisit
{
    public class SevenBitUnPackVisitier : IVisitier
    {
        private ArraySegment<byte> _data;
        private int _pos;

        private byte PeekByte() => _data[0];
        private byte PeekByte(uint offset) =>_data[_pos + (int)offset];
        private byte UnPackByte()=>_data[_pos++];
        private void Skip(uint n) =>_pos += (int)n;
        private void MovePos(int offset)
        {
            var newPos = _pos + offset;
            if (newPos < 0 || newPos > _data.Count)
            {
                throw new Exception("SevenBitUnPack end of data");
            }
            _pos = newPos;
        }

        private uint PeekNumber(out uint val)
        {
            uint n = 1;
            val = (uint)(PeekByte() & 0x7f);
            while (PeekByte(n - 1) > 0x7f)
            {
                uint hi = (uint)(PeekByte(n) & 0x7f);
                val |= (uint)((int)hi << (7 * (int)n));
                ++n;
            }
            return n;
        }
        public uint PeekNumber(out ulong val)
        {
            uint n = 1;
            val = (ulong)(PeekByte() & 0x7f);
            while (PeekByte(n - 1) > 0x7f)
            {
                ulong hi = (ulong)(PeekByte(n) & 0x7f);
                val |= (ulong)(hi << (7 * (int)n));
                ++n;
            }
            return n;
        }

        private uint PeekHeader(out uint tag, out SevenBitDataType type)
        {
            uint n = 1;
            var val = PeekByte();
            type = (SevenBitDataType)(val >> 4);
            tag = (uint)(val & 0xf); 
            if (tag == 0xf)
            {
                MovePos(1);
                n += PeekNumber(out tag);
                MovePos(-1);
            }
            return n;
        }
        private void UnPackHeader(out uint tag, out SevenBitDataType type)
        {
            var val = UnPackByte();
            type = (SevenBitDataType)(val >> 4);
            tag = (uint)(val & 0xf);
            if (tag == 0xf)
            {
                UnPackNumber(out tag);
            }
        }
        private void UnPackNumber(out uint v)
        {
            uint n = PeekNumber(out v);
            Skip(n);
        }
        private void UnPackNumber(out ulong v)
        {
            uint n = PeekNumber(out v);
            Skip(n);
        }
        public void SkipField()
        {
            UnPackHeader(out uint _, out SevenBitDataType curtype);
            SkipField(curtype);
        }
        public void SkipToStructEnd()
        {
            while (true)
            {
                UnPackHeader(out uint _, out SevenBitDataType curtype);
                if (curtype == SevenBitDataType.StructEnd)
                {
                    break;
                }
                SkipField(curtype);
            }
        }
        private void SkipField(SevenBitDataType type)
        {
            switch (type)
            {
                case SevenBitDataType.Positive:
                case SevenBitDataType.Negative:
                case SevenBitDataType.Float:
                case SevenBitDataType.Double:
                    {
                        UnPackNumber(out ulong val);
                    }
                    break;
                case SevenBitDataType.String:
                    {
                        UnPackNumber(out uint size);
                        Skip(size);
                    }
                    break;
                case SevenBitDataType.Vector:
                    {
                        UnPackNumber(out uint size);
                        for (UInt32 i = 0; i < size; ++i)
                        {
                            SkipField();
                        }
                    }
                    break;
                case SevenBitDataType.Map:
                    {
                        UnPackNumber(out uint size);
                        for (UInt32 i = 0; i < size; ++i)
                        {
                            SkipField();
                            SkipField();
                        }
                    }
                    break;
                case SevenBitDataType.StructBegin:
                    SkipToStructEnd();
                    break;
                case SevenBitDataType.StructEnd:
                    break;
                default:
                    throw new Exception($"SevenBitUnPack unknown type : {type}");
            }
        }
        private bool SkipToTag(uint tag)
        {
            while(_pos < _data.Count)
            {
                var n = PeekHeader(out uint curtag, out SevenBitDataType type);
                if (type == SevenBitDataType.StructEnd || curtag > tag)
                    break;
                if (curtag == tag)
                    return true;
                Skip(n);
                SkipField(type);
            }
            return false;
        }

        private uint UnPackUInt(uint tag)
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Positive)
                {
                    UnPackNumber(out uint v);
                    return v;
                }
                else if (type == SevenBitDataType.Negative)
                {
                    UnPackNumber(out uint v);
                    return (uint)(-((int)v));
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            return 0;
        }

        private ulong UnPackULong(uint tag)
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Positive)
                {
                    UnPackNumber(out ulong v);
                    return v;
                }
                else if (type == SevenBitDataType.Negative)
                {
                    UnPackNumber(out ulong v);
                    return (ulong)(-((long)v));
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            return 0;
        }

        public void Visit(uint tag, string name, bool require, ref bool value)
        {
            var v = UnPackUInt(tag);
            value = v != 0;
        }

        public void Visit(uint tag, string name, bool require, ref bool[] value)
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.String)
                {
                    UnPackNumber(out uint size);
                    if(value == null || value.Length != size)
                    {
                        value = new bool[size];
                    }
                    for (int i = 0; i < size; i++)
                    {
                        value[i] = _data.Array[_data.Offset + _pos + i] != 0;
                    }
                    Skip(size);
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            value = EmptyArray<bool>.Array;
        }

        public void Visit(uint tag, string name, bool require, ref byte value)
        {
            value = (byte)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, bool require, ref byte[] value)
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.String)
                {
                    UnPackNumber(out uint size);
                    if (value == null || value.Length != size)
                    {
                        value = new byte[size];
                    }
                    Array.Copy(_data.Array, _data.Offset + _pos, value, 0, size);
                    Skip(size);
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            value = EmptyArray<byte>.Array;
        }

        public void Visit(uint tag, string name, bool require, ref sbyte value)
        {
            value = (sbyte)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, bool require, ref sbyte[] value)
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.String)
                {
                    UnPackNumber(out uint size);
                    if (value == null || value.Length != size)
                    {
                        value = new sbyte[size];
                    }
                    for (int i = 0; i < size; i++)
                    {
                        value[i] = (sbyte)_data.Array[_data.Offset + _pos + i];
                    }
                    Skip(size);
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            value = EmptyArray<sbyte>.Array;
        }

        public void Visit(uint tag, string name, bool require, ref short value)
        {
            value = (short)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, bool require, ref ushort value)
        {
            value = (ushort)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, bool require, ref int value)
        {
            value = (int)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, bool require, ref uint value)
        {
            value = UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, bool require, ref long value)
        {
            value = (long)UnPackULong(tag);
        }

        public void Visit(uint tag, string name, bool require, ref ulong value)
        {
            value = UnPackULong(tag);
        }

        public void Visit(uint tag, string name, bool require, ref float value)
        {
            value = 0;
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if(type == SevenBitDataType.Float)
                {
                    UnPackNumber(out uint v);
                    unsafe
                    {
                        value = *((float*)(&v));
                    }
                }
                else if (type == SevenBitDataType.Double)
                {
                    UnPackNumber(out ulong v);
                    unsafe
                    {
                        double d = *((double*)(&v));
                        value = (float)d;
                    }
                }
            }
        }

        public void Visit(uint tag, string name, bool require, ref double value)
        {
            value = 0;
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Double)
                {
                    UnPackNumber(out ulong v);
                    unsafe
                    {
                        value = *((double*)(&v));
                    }
                }
                else if (type == SevenBitDataType.Float)
                {
                    UnPackNumber(out uint v);
                    unsafe
                    {
                        float f = *((float*)(&v));
                        value = (double)f;
                    }
                }
            }
        }

        public void Visit(uint tag, string name, bool require, ref string value)
        {
            value = string.Empty;
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.String)
                {
                    UnPackNumber(out uint size);
                    value = System.Text.Encoding.UTF8.GetString(_data.Array, _data.Offset + _pos, (int)size);
                    Skip(size);
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }

        public void VisitArray<T>(uint tag, string name, bool require, ref T[] value)
        {
            if (SkipToTag(tag)) 
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Vector)
                {
                    UnPackNumber(out uint size);
                    if (value == null || value.Length != size)
                    {
                        value = new T[size];
                    }
                    if (TypeVisit<T>.IsCustomStruct)
                    {
                        for (uint i = 0; i < size; i++)
                        {
                            T item = TypeVisit<T>.New();
                            UnPackHeader(out uint _, out SevenBitDataType fieldType);
                            if (fieldType != SevenBitDataType.StructEnd)
                                ThrowIncompatibleType(fieldType);
                            TypeVisit<T>.Visit(this, 0, string.Empty, false, ref item);
                            SkipToStructEnd();
                            value[i] = item;
                        }
                    }
                    else
                    {
                        for (uint i = 0; i < size; i++)
                        {
                            TypeVisit<T>.Visit(this, 0, string.Empty, false, ref value[i]);
                        }
                    }
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            value = EmptyArray<T>.Array;
        }

        public void VisitDictionary<TKey, TValue>(uint tag, string name, bool require, ref Dictionary<TKey, TValue> value)
        {
            value?.Clear();
            if(SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if(type == SevenBitDataType.Map)
                {
                    value ??= new Dictionary<TKey, TValue>();
                    UnPackNumber(out uint size);
                    if(TypeVisit<TValue>.IsCustomStruct)
                    {

                        for (uint i = 0; i < size; i++)
                        {
                            TKey key = default;
                            TypeVisit<TKey>.Visit(this, 0, string.Empty, false, ref key);

                            TValue val = TypeVisit<TValue>.New();
                            UnPackHeader(out uint _, out SevenBitDataType fieldType);
                            if (fieldType != SevenBitDataType.StructEnd)
                                TypeVisit<TValue>.Visit(this, 0, string.Empty, false, ref val);
                            SkipToStructEnd();

                            value.Add(key, val);
                        }
                    }
                    else
                    {
                        for (uint i = 0; i < size; i++)
                        {
                            TKey key = default;
                            TValue val = TypeVisit<TValue>.New();
                            TypeVisit<TKey>.Visit(this, 0, string.Empty, false, ref key);
                            TypeVisit<TValue>.Visit(this, 0, string.Empty, false, ref val);
                            value.Add(key, val);
                        }

                    }
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }

        public void VisitEnum<T>(uint tag, string name, bool require, ref T value) where T : Enum
        {
            ulong v = UnPackULong(tag);
            value = (T)Enum.ToObject(typeof(T), v);
        }

        public void VisitHashSet<T>(uint tag, string name, bool require, ref HashSet<T> value)
        {
            value?.Clear();
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Vector)
                {
                    UnPackNumber(out uint size);
                    value ??= new HashSet<T>();
                    if(TypeVisit<T>.IsCustomStruct)
                    {
                        for (uint i = 0; i < size; i++)
                        {
                            T item = TypeVisit<T>.New();
                            UnPackHeader(out uint _, out SevenBitDataType fieldType);
                            if (fieldType != SevenBitDataType.StructEnd)
                                ThrowIncompatibleType(fieldType);
                            TypeVisit<T>.Visit(this, 0, string.Empty, false, ref item);
                            SkipToStructEnd();
                            value.Add(item);
                        }
                    }
                    else
                    {
                        for (uint i = 0; i < size; i++)
                        {
                            T item = TypeVisit<T>.New();
                            TypeVisit<T>.Visit(this, 0, string.Empty, false, ref item);
                            value.Add(item);
                        }
                    }
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }

        public void VisitList<T>(uint tag, string name, bool require, ref List<T> value)
        {
            value?.Clear();
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Vector)
                {
                    UnPackNumber(out uint size);
                    value ??= new List<T>();
                    if (TypeVisit<T>.IsCustomStruct)
                    {
                        for (uint i = 0; i < size; i++)
                        {
                            T item = TypeVisit<T>.New();
                            
                            UnPackHeader(out uint _, out SevenBitDataType fieldType);
                            if(fieldType != SevenBitDataType.StructEnd)
                                ThrowIncompatibleType(fieldType);
                            TypeVisit<T>.Visit(this, 0, string.Empty, false, ref item);
                            SkipToStructEnd();

                            value.Add(item);
                        }
                    }
                    else
                    {
                        for (uint i = 0; i < size; i++)
                        {
                            T item = TypeVisit<T>.New();
                            TypeVisit<T>.Visit(this, 0, string.Empty, false, ref item);
                            value.Add(item);
                        }

                    }
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }

        public void VisitStruct<T>(uint tag, string name, bool require, ref T value) where T : struct
        {
            if(SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.StructBegin)
                {
                    TypeVisit<T>.Visit(this, 0, string.Empty, false, ref value);
                    SkipToStructEnd();
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            //value = default;
        }

        public void VisitClass<T>(uint tag, string name, bool require, ref T value) where T : class, new()
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.StructBegin)
                {
                    value ??= new T();
                    TypeVisit<T>.Visit(this, 0, string.Empty, false, ref value);
                    SkipToStructEnd();
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
            //暂时不重置，减少不必要的对象创建
            //value = new T();
        }

        public static void ThrowIncompatibleType(SevenBitDataType type)
        {
            throw new Exception(string.Format("got wrong type {0}", type));
        }


    }
}
