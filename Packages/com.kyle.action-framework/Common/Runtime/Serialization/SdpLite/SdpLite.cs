using System;
using System.IO;
using System.Text;

public class SdpLite
{
    public enum DataType
    {
        Positive = 0,
        Negative = 1,
        Float = 2,
        Double = 3,
        String = 4,
        Vector = 5,
        Map = 6,
        StructBegin = 7,
        StructEnd = 8,
    }

    public struct Header
    {
        public uint tag;
        public DataType type;
    }
    public static class Converter
    {
        public static unsafe int SingleToInt32Bits(float value)
        {
            return *(int*)(&value);
        }
        public static unsafe uint SingleToUInt32Bits(float value)
        {
            return *(uint*)(&value);
        }
        public static unsafe float UInt32BitsToSingle(uint value)
        {
            return *(float*)(&value);
        }
    }
    public class SdpLiteException : Exception
    {
        private readonly StringBuilder _sb = new StringBuilder();
        public SdpLiteException(string message)
        {
            _sb.Append(message);
        }

        public SdpLiteException(string message, uint tag, string name)
        {
            _sb.Append(message);
            Trace(tag, name);
        }

        public void Trace(uint tag, string name)
        {
            if (name != null)
            {
                if (_sb.Length != 0)
                {
                    _sb.Append(" <- ");
                }
                _sb.AppendFormat("{0}:{1}", tag, name);
            }
        }

        public override string Message
        {
            get { return _sb.ToString(); }
        }
    }

    public class Packer
    {
        private static Packer gDefault;

        private static System.Threading.Thread mainThread;

        public static Packer GetResetDefault()
        {
            if (mainThread == null)
            {
                mainThread = System.Threading.Thread.CurrentThread;
            }
            else if (mainThread != System.Threading.Thread.CurrentThread)
            {
                throw new InvalidOperationException("Calling GetResetDefault from other thread!!!");
            }
            gDefault.Reset();
            return gDefault;
        }

        static Packer()
        {
            gDefault = new Packer();
        }

        private MemoryStream _memory;

        private byte[] _bytes = new byte[10];

        public Packer()
        {
            _memory = new MemoryStream();

        }

        public long Position
        {
            get
            {
                return _memory.Position;
            }
        }

        public void Rewind(long p)
        {
            _memory.Position = p;
            _memory.SetLength(p);
        }

        public Packer(MemoryStream memory)
        {
            _memory = memory;
        }

        public void Reset()
        {
            _memory.Position = 0;
            _memory.SetLength(0);
        }

        public void Reset(MemoryStream memory)
        {
            _memory = memory;
        }

        public byte[] ToBytes()
        {
            return _memory.ToArray();
        }

        public MemoryStream GetStream() { return _memory; }

        public void Pack(uint tag, bool val)
        {
            uint v = (uint)(val ? 1 : 0);
            Pack(tag, v);
        }

        public void Pack(uint tag, byte val)
        {
            uint v = val;
            Pack(tag, v);
        }

        public void Pack(uint tag, sbyte val)
        {
            int v = val;
            Pack(tag, v);
        }

        public void Pack(uint tag, short val)
        {
            int v = val;
            Pack(tag, v);
        }

        public void Pack(uint tag, ushort val)
        {
            uint v = val;
            Pack(tag, v);
        }

        public void Pack(uint tag, float val)
        {
            PackHeader(tag, DataType.Float);
            Pack(val);
        }

        public void Pack(float val)
        {
            Pack(Converter.SingleToUInt32Bits(val));
        }

        public void Pack(uint tag, double val)
        {
            PackHeader(tag, DataType.Double);
            Pack(val);
        }

        public void Pack(double val)
        {
            Pack((ulong)BitConverter.DoubleToInt64Bits(val));
        }
        public void Pack(uint tag, int val)
        {
            if (val < 0)
            {
                PackHeader(tag, DataType.Negative);
                Pack((uint)-val);
            }
            else
            {
                uint v = (uint)val;
                Pack(tag, v);
            }
        }

        public void Pack(uint tag, uint val)
        {
            PackHeader(tag, DataType.Positive);
            Pack(val);
        }

        public void Pack(uint tag, long val)
        {
            if (val < 0)
            {
                PackHeader(tag, DataType.Negative);
                Pack((ulong)-val);
            }
            else
            {
                ulong v = (ulong)val;
                Pack(tag, v);
            }
        }

        public void Pack(uint tag, ulong val)
        {
            PackHeader(tag, DataType.Positive);
            Pack(val);
        }

        public void PackHeader(uint tag, DataType type)
        {
            byte header = (byte)((byte)type << 4);
            if (tag < 15)
            {
                header |= (byte)tag;
                _memory.WriteByte(header);
            }
            else
            {
                header |= 0xf;
                _memory.WriteByte(header);
                Pack(tag);
            }
        }

        public void Pack(uint val)
        {
            int n = 0;
            while (val > 0x7f)
            {
                _bytes[n++] = (byte)((val & 0x7f) | 0x80);
                val >>= 7;
            }
            _bytes[n++] = (byte)val;
            _memory.Write(_bytes, 0, n);
        }

        public void Pack(byte val)
        {
            _memory.WriteByte(val);
        }

        public void Pack(ulong val)
        {
            int n = 0;
            while (val > 0x7f)
            {
                _bytes[n++] = (byte)((val & 0x7f) | 0x80);
                val >>= 7;
            }
            _bytes[n++] = (byte)val;
            _memory.Write(_bytes, 0, n);
        }

        public void Pack(byte[] bytes, int offset, int length)
        {
            _memory.Write(bytes, offset, length);
        }

        public void Pack(string str, uint bytesLen)
        {
            if(bytesLen > _bytes.Length)
            {
                Array.Resize(ref _bytes, (int)bytesLen);
            }
            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, _bytes, 0);
            _memory.Write(_bytes, 0, (int)bytesLen);
        }

    }

    public class Unpacker
    {
#if !IS_SERVER
        private static System.Threading.Thread mainThread;
#endif
        private static void CheckThread()
        {
#if !IS_SERVER
            if (mainThread == null)
            {
                mainThread = System.Threading.Thread.CurrentThread;
            }
            else if (mainThread != System.Threading.Thread.CurrentThread)
            {
                throw new Exception("network - Calling GetResetDefault from other thread!!!");
            }
#endif
        }

        public static Unpacker GetResetDefault(byte[] data)
        {
            CheckThread();
            gDefault.Reset(data);
            return gDefault;
        }
        public static Unpacker GetResetDefault(byte[] data, int offset)
        {
            CheckThread();
            gDefault.Reset(data, offset);
            return gDefault;
        }
        public static Unpacker GetResetDefault(byte[] data, int offset, int count)
        {
            CheckThread();
            gDefault.Reset(data, offset, count);
            return gDefault;
        }

        static Unpacker()
        {
            gDefault = new Unpacker();
        }

        private static Unpacker gDefault;

        private byte[] _data;
        private int _position;
        private int _end; 

        public Unpacker()
        {
        }

        public Unpacker(byte[] data)
        {
            Reset(data);
        }

        public Unpacker(byte[] data, int offset)
        {
            Reset(data, offset);
        }
        public Unpacker(byte[] data, int offset, int count)
        {
            Reset(data, offset, count);
        }

        public bool Empty { get { return _end == 0 || _data == null; } }
        public void Reset(byte[] data)
        {
            Reset(data, 0);
        }
        public void Reset(byte[] data, int offset)
        {
            Reset(data, offset, data.Length);
        }
        public void Reset(byte[] data, int offset, int count)
        {
            _data = data;
            _position = offset;
            _end = offset + count;
        }

        public void Unpack(DataType type, out bool value)
        {
            uint v = UnpackUInt();
            value = v != 0;
        }

        public void Unpack(DataType type, out sbyte value)
        {
            int v = UnpackInt(type);
            value = (sbyte)v;
        }

        public void Unpack(DataType type, out byte value)
        {
            uint v = UnpackUInt();
            value = (byte)v;
        }
        public void Unpack(DataType type, out short value)
        {
            int v = UnpackInt(type);
            value = (short)v;
        }

        public void Unpack(DataType type, out ushort value)
        {
            uint v = UnpackUInt();
            value = (ushort)v;
        }

        public void Unpack(DataType type, out int value)
        {
            value = UnpackInt(type);
        }

        public void Unpack(DataType type, out uint value)
        {
            value = UnpackUInt();
        }

        public void Unpack(DataType type, out long value)
        {
            value = UnpackLong(type);
        }

        public void Unpack(DataType type, out ulong value)
        {
            value = UnpackULong();
        }

        public void Unpack(DataType type, out float value)
        {
            value = UnpackFloat(type);
        }

        public void Unpack(DataType type, out double value)
        {
            value = UnpackDouble(type);
        }

        public int UnpackInt(DataType type)
        {
            uint val = 0;
            SkipBytes(Peek(out val));
            if (type == DataType.Negative)
            {
                return -(int)val;
            }
            else if (type == DataType.Positive)
            {
                return (int)val;
            }
            else
            {
                ThrowIncompatibleType(type);
            }
            return 0;
        }

        public long UnpackLong(DataType type)
        {
            ulong val = 0;
            SkipBytes(Peek(out val));
            if (type == DataType.Negative)
            {
                return -(long)val;
            }
            else if (type == DataType.Positive)
            {
                return (long)val;
            }
            else
            {
                ThrowIncompatibleType(type);
            }
            return 0;
        }

        public float UnpackFloat(DataType type)
        {
            float val = 0.0f;
            if (type == DataType.Float)
            {
                uint v;
                v = UnpackUInt();
                val = Converter.UInt32BitsToSingle(v);
            }
            else if (type == DataType.Double)
            {
                ulong v = UnpackULong();
                val = (float)BitConverter.Int64BitsToDouble((long)v);
            }
            else
            {
                ThrowIncompatibleType(type);
            }
            return val;
        }

        public double UnpackDouble(DataType type)
        {
            double val = 0.0f;
            if (type == DataType.Float)
            {
                uint v;
                v = UnpackUInt();
                val = Converter.UInt32BitsToSingle(v); ;
            }
            else if (type == DataType.Double)
            {
                ulong v = UnpackULong();
                val = BitConverter.Int64BitsToDouble((long)v);
            }
            else
            {
                ThrowIncompatibleType(type);
            }
            return val;
        }

        public static void ThrowIncompatibleType(DataType type)
        {
            throw new SdpLiteException(string.Format("got wrong type {0}", type));
        }

        protected static void ThrowFieldNotExist()
        {
            throw new SdpLiteException("field not exist");
        }

        protected static void ThrowNoEnoughData()
        {
            throw new SdpLiteException("end of data");
        }

        protected static void ThrowUnknownDataType(DataType type)
        {
            throw new SdpLiteException(string.Format("unknown type {0}", (int)type));
        }

        internal void CheckSize(uint size)
        {
            if (_end - _position < size)
            {
                ThrowNoEnoughData();
            }
        }

        private uint Peek(out uint val)
        {
            uint size = 1;
            CheckSize(1);
            val = (uint)(_data[_position] & 0x7f);
            while (_data[_position + size - 1] > 0x7f)
            {
                CheckSize(size + 1);
                uint hi = (uint)(_data[_position + size] & 0x7f);
                val |= hi << (int)(7 * size);
                ++size;
            }
            //if (size > 4)
            //{
            //    ILog.Debug("bad int compress " + size + " value: " + val);
            //}
            return size;
        }

        public uint UnpackUInt()
        {
            uint val = 0;
            SkipBytes(Peek(out val));
            return val;
        }

        public byte UnpackByte()
        {
            return _data[_position++];
        }

        public ulong UnpackULong()
        {
            ulong val = 0;
            SkipBytes(Peek(out val));
            return val;
        }

        private uint Peek(out ulong val)
        {
            uint size = 1;
            CheckSize(1);
            val = (ulong)(_data[_position] & 0x7f);
            while (_data[_position + size - 1] > 0x7f)
            {
                CheckSize(size + 1);
                ulong hi = (ulong)(_data[_position + size] & 0x7f);
                val |= hi << (int)(7 * size);
                ++size;
            }
            return size;
        }

        public uint PeekHeader(out Header header)
        {
            uint size = 1;
            CheckSize(1);
            header.type = (DataType)(_data[_position] >> 4);
            header.tag = (uint)(_data[_position] & 0xf);
            if (header.tag == 0xf)
            {
                _position += 1;
                size += Peek(out header.tag);
                _position -= 1;
            }
            return size;
        }

        public Header UnpackHeader()
        {
            Header header = new Header();
            SkipBytes(PeekHeader(out header));
            return header;
        }

        public void SkipBytes(uint size)
        {
            CheckSize(size);
            _position += (int)size;
        }

        public void SkipField(DataType type)
        {
            switch (type)
            {
                case DataType.Positive:
                case DataType.Negative:
                case DataType.Float:
                case DataType.Double:
                    {
                        UnpackULong();
                        break;
                    }
                case DataType.String:
                    {
                        uint size = UnpackUInt();
                        SkipBytes(size);
                        break;
                    }
                case DataType.Vector:
                    {
                        uint size = UnpackUInt();
                        for (uint i = 0; i < size; ++i)
                        {
                            SkipField();
                        }
                        break;
                    }
                case DataType.Map:
                    {
                        uint size = UnpackUInt();
                        for (uint i = 0; i < size; ++i)
                        {
                            SkipField();
                            SkipField();
                        }
                        break;
                    }
                case DataType.StructBegin:
                    SkipToStructEnd();
                    break;
                case DataType.StructEnd:
                    break;
                default:
                    ThrowUnknownDataType(type);
                    break;
            }
        }

        public void SkipField()
        {
            var header = UnpackHeader();
            SkipField(header.type);
        }

        public bool SkipToTag(uint tag, bool require)
        {
            while (_position < _end)
            {
                Header header;
                uint size = PeekHeader(out header);
                if (header.type == DataType.StructEnd || header.tag > tag)
                {
                    break;
                }
                if (header.tag == tag)
                {
                    return true;
                }
                SkipBytes(size);
                SkipField(header.type);
            }
            if (require)
            {
                ThrowFieldNotExist();
            }
            return false;
        }

        public void SkipToStructEnd()
        {
            while (true)
            {
                var header = UnpackHeader();
                if (header.type == DataType.StructEnd)
                {
                    break;
                }
                SkipField(header.type);
            }
        }

        public void Read(byte[] bytes, int offset, int size)
        {
            Buffer.BlockCopy(_data, _position, bytes, offset, size);
            SkipBytes((uint)size);
        }

        public void Read(uint bytesLen, out string str)
        {
            str = System.Text.Encoding.UTF8.GetString(_data, _position, (int)bytesLen);
            SkipBytes(bytesLen);
        }
    }
}
