using System;
using System.Collections.Generic;

namespace DataVisit
{
    public class SevenBitUnPackVisitier : SevenBitBase, IVisitier
    {
        private ArraySegment<byte> _data;
        private int _pos;

        public SevenBitUnPackVisitier(ArraySegment<byte> data)
        {
            _data = data;
            _pos = 0;
        }

        public SevenBitUnPackVisitier(byte[] data) : this(new ArraySegment<byte>(data)) { }

        private void EnsureIndex(int index)
        {
            if (index < 0 || index >= _data.Count)
            {
                throw new Exception("SevenBitUnPack end of data");
            }
        }

        private byte PeekByte()
        {
            EnsureIndex(_pos);
            return _data[_pos];
        }
        private byte PeekByte(uint offset)
        {
            var index = _pos + (int)offset;
            EnsureIndex(index);
            return _data[index];
        }
        private byte UnPackByte()
        {
            var val = PeekByte();
            _pos++;
            return val;
        }
        private void Skip(uint n)
        {
            var newPos = _pos + (int)n;
            if (newPos < 0 || newPos > _data.Count)
            {
                throw new Exception("SevenBitUnPack end of data");
            }
            _pos = newPos;
        }
        private void MovePos(int offset)
        {
            var newPos = _pos + offset;
            if (newPos < 0 || newPos >= _data.Count)
            {
                throw new Exception("SevenBitUnPack end of data");
            }
            _pos = newPos;
        }

        private uint PeekNumber(out uint val)
        {
            uint n = 0;
            byte b = PeekByte(n);
            val = (uint)(b & 0x7f);
            while ((b & 0x80) != 0)
            {
                if (n >= 4)
                {
                    throw new Exception("SevenBitUnPack number is too large");
                }
                n++;
                b = PeekByte(n);
                uint hi = (uint)(b & 0x7f);
                val |= (uint)(hi << (7 * (int)n));
            }
            return n + 1;
        }
        public uint PeekNumber(out ulong val)
        {
            uint n = 0;
            byte b = PeekByte(n);
            val = (ulong)(b & 0x7f);
            while ((b & 0x80) != 0)
            {
                if (n >= 9)
                {
                    throw new Exception("SevenBitUnPack number is too large");
                }
                n++;
                b = PeekByte(n);
                ulong hi = (ulong)(b & 0x7f);
                val |= (ulong)(hi << (7 * (int)n));
            }
            return n + 1;
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
                case SevenBitDataType.StructBegin:
                    SkipToStructEnd();
                    break;
                case SevenBitDataType.DynamicBegin:
                    SkipField();
                    UnPackHeader(out uint _, out SevenBitDataType dynamicType);
                    SkipField(dynamicType);
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

        public void Visit(uint tag, string name, uint flag, ref bool value)
        {
            var v = UnPackUInt(tag);
            value = v != 0;
        }

        public void Visit(uint tag, string name, uint flag, ref bool[] value)
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

        public void Visit(uint tag, string name, uint flag, ref byte value)
        {
            value = (byte)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref byte[] value)
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

        public void Visit(uint tag, string name, uint flag, ref sbyte value)
        {
            value = (sbyte)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref sbyte[] value)
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

        public void Visit(uint tag, string name, uint flag, ref short value)
        {
            value = (short)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref ushort value)
        {
            value = (ushort)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref int value)
        {
            value = (int)UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref uint value)
        {
            value = UnPackUInt(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref long value)
        {
            value = (long)UnPackULong(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref ulong value)
        {
            value = UnPackULong(tag);
        }

        public void Visit(uint tag, string name, uint flag, ref float value)
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

        public void Visit(uint tag, string name, uint flag, ref double value)
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

        public void Visit(uint tag, string name, uint flag, ref string value)
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

        public void VisitEnum<T>(uint tag, string name, uint flag, ref T value) where T : Enum
        {
            ulong v = UnPackULong(tag);
            value = (T)Enum.ToObject(typeof(T), v);
        }

        public void VisitStruct<T>(uint tag, string name, uint flag, ref T value) where T : struct
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.StructBegin)
                {
                    TypeVisitT<T>.Visit(this, 0, string.Empty, flag & UnRequiredFlag, ref value);
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

        public void VisitClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.StructBegin)
                {
                    if(value == null || value.GetType() != typeof(T))
                        value = new T();
                    TypeVisitT<T>.Visit(this, 0, string.Empty, flag & UnRequiredFlag, ref value);
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
        public void VisitDynamicClass<T>(uint tag, string name, uint flag, ref T value) where T : class, new()
        {
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.DynamicBegin)
                {
                    int typeId = 0;
                    Visit(0, string.Empty, flag & (~RequiredFlag), ref typeId);
                    var visit = TypeVisit.GetVisit(typeId);
                    UnPackHeader(out uint _, out SevenBitDataType structType);
                    if(visit == null)
                    {
                        //如果对应的ID没有注册类型，就跳过这个结构体，说明改类型被删除。
                        value = null;
                        SkipField(structType);
                        return;
                    }
                    if (structType != SevenBitDataType.StructBegin)
                        ThrowIncompatibleType(structType);
                    object obj = value;
                    visit(this, 1, string.Empty, flag & UnRequiredFlag, ref obj);
                    value = (T)obj;
                    SkipToStructEnd();

                    SkipToStructEnd();
                    return;
                }
                else if(type == SevenBitDataType.StructBegin)
                {
                    value = null;
                    SkipToStructEnd();
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }
        public void VisitArray<T>(uint tag, string name, uint flag, ref T[] value)
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
                    for (uint i = 0; i < size; i++)
                    {
                        if (TypeVisitT<T>.IsCustomStruct)
                        {
                            UnPackHeader(out uint _, out SevenBitDataType fieldType);
                            if (fieldType != SevenBitDataType.StructBegin)
                                ThrowIncompatibleType(fieldType);
                        }
                        TypeVisitT<T>.Visit(this, 0, string.Empty, flag & UnRequiredFlag, ref value[i]);
                        if (TypeVisitT<T>.IsCustomStruct)
                            SkipToStructEnd();
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
        public void VisitDynamicArray<T>(uint tag, string name, uint flag, ref T[] value) where T : class, new()
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
                    for (uint i = 0; i < size; i++)
                    {
                        VisitDynamicClass(0, string.Empty, flag & UnRequiredFlag, ref value[i]);
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

        public void VisitDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value)
        {
            value?.Clear();
            if(SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if(type == SevenBitDataType.Vector)
                {
                    value ??= new Dictionary<TKey, TValue>();
                    UnPackNumber(out uint size);
                    for (uint i = 0; i < size; i++)
                    {
                        UnPackHeader(out uint _, out SevenBitDataType t);
                        if (t == SevenBitDataType.StructBegin)
                        {
                            TKey key = default;
                            TValue val = TypeVisitT<TValue>.New();
                            TypeVisitT<TKey>.Visit(this, 0, string.Empty, flag & UnRequiredFlag, ref key);
                            if (TypeVisitT<TValue>.IsCustomStruct)
                            {
                                UnPackHeader(out uint _, out SevenBitDataType valueType);
                                if (valueType != SevenBitDataType.StructBegin)
                                    ThrowIncompatibleType(valueType);
                            }
                            TypeVisitT<TValue>.Visit(this, 1, string.Empty, flag & UnRequiredFlag, ref val);
                            if (TypeVisitT<TValue>.IsCustomStruct)
                                SkipToStructEnd();
                            value.Add(key, val);
                            SkipToStructEnd();
                        }
                    }
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }
        public void VisitDynamicDictionary<TKey, TValue>(uint tag, string name, uint flag, ref Dictionary<TKey, TValue> value) where TValue : class, new()
        {
            value?.Clear();
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Vector)
                {
                    value ??= new Dictionary<TKey, TValue>();
                    UnPackNumber(out uint size);
                    for (uint i = 0; i < size; i++)
                    {
                        UnPackHeader(out uint _, out SevenBitDataType t);
                        if (t == SevenBitDataType.StructBegin)
                        {
                            TKey key = default;
                            TypeVisitT<TKey>.Visit(this, 0, string.Empty, flag & UnRequiredFlag, ref key);
                            TValue val = default;
                            VisitDynamicClass(1, string.Empty, flag & UnRequiredFlag, ref val);
                            value.Add(key, val);
                            SkipToStructEnd();
                        }
                    }
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }

        public void VisitHashSet<T>(uint tag, string name, uint flag, ref HashSet<T> value)
        {
            value?.Clear();
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Vector)
                {
                    UnPackNumber(out uint size);
                    value ??= new HashSet<T>();

                    for (uint i = 0; i < size; i++)
                    {
                        if (TypeVisitT<T>.IsCustomStruct)
                        {
                            UnPackHeader(out uint _, out SevenBitDataType fieldType);
                            if (fieldType != SevenBitDataType.StructBegin)
                                ThrowIncompatibleType(fieldType);
                        }
                        T item = TypeVisitT<T>.New();
                        TypeVisitT<T>.Visit(this, 0, string.Empty, flag & UnRequiredFlag, ref item);
                        if (TypeVisitT<T>.IsCustomStruct)
                            SkipToStructEnd();
                        value.Add(item);
                    }
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }

        public void VisitList<T>(uint tag, string name, uint flag, ref List<T> value)
        {
            value?.Clear();
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Vector)
                {
                    UnPackNumber(out uint size);
                    value ??= new List<T>();
                    for (uint i = 0; i < size; i++)
                    {
                        if (TypeVisitT<T>.IsCustomStruct)
                        {
                            UnPackHeader(out uint _, out SevenBitDataType fieldType);
                            if (fieldType != SevenBitDataType.StructBegin)
                                ThrowIncompatibleType(fieldType);
                        }
                        T item = TypeVisitT<T>.New();
                        TypeVisitT<T>.Visit(this, 0, string.Empty, flag & UnRequiredFlag, ref item);
                        if (TypeVisitT<T>.IsCustomStruct)
                            SkipToStructEnd();
                        value.Add(item);
                    }
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }
        public void VisitDynamicList<T>(uint tag, string name, uint flag, ref List<T> value) where T : class, new()
        {
            value?.Clear();
            if (SkipToTag(tag))
            {
                UnPackHeader(out uint _, out SevenBitDataType type);
                if (type == SevenBitDataType.Vector)
                {
                    UnPackNumber(out uint size);
                    value ??= new List<T>();
                    for (uint i = 0; i < size; i++)
                    {
                        T item = default;
                        VisitDynamicClass(0, string.Empty, flag & UnRequiredFlag, ref item);
                        value.Add(item);
                    }
                    return;
                }
                else
                {
                    ThrowIncompatibleType(type);
                }
            }
        }

        public static void ThrowIncompatibleType(SevenBitDataType type)
        {
            throw new Exception(string.Format("got wrong type {0}", type));
        }


    }
}
