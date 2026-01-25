namespace DataVisit
{
    public class InnerTypeVisit
    {
        private static bool _isRegistered = false;
        public static void Register()
        {
            if (_isRegistered)return;
            _isRegistered = true;
            TypeVisitT<bool>.VisitFunc = VisitBoolean;
            TypeVisitT<byte>.VisitFunc = VisitByte;
            TypeVisitT<sbyte>.VisitFunc = VisitSByte;
            TypeVisitT<short>.VisitFunc = VisitInt16;
            TypeVisitT<ushort>.VisitFunc = VisitUInt16;
            TypeVisitT<int>.VisitFunc = VisitInt32;
            TypeVisitT<uint>.VisitFunc = VisitUInt32;
            TypeVisitT<long>.VisitFunc = VisitInt64;
            TypeVisitT<ulong>.VisitFunc = VisitUInt64;
            TypeVisitT<float>.VisitFunc = VisitFloat;
            TypeVisitT<double>.VisitFunc = VisitDouble;
            TypeVisitT<string>.VisitFunc = VisitString;
            TypeVisitT<bool[]>.VisitFunc = VisitBooleanArray;
            TypeVisitT<byte[]>.VisitFunc = VisitByteArray;
            TypeVisitT<sbyte[]>.VisitFunc = VisitSByteArray;
        }
        private static void VisitBoolean(IVisitier visitier, uint tag, string name, uint flag, ref bool value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitByte(IVisitier visitier, uint tag, string name, uint flag, ref byte value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitSByte(IVisitier visitier, uint tag, string name, uint flag, ref sbyte value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitInt16(IVisitier visitier, uint tag, string name, uint flag, ref short value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitUInt16(IVisitier visitier, uint tag, string name, uint flag, ref ushort value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitInt32(IVisitier visitier, uint tag, string name, uint flag, ref int value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitUInt32(IVisitier visitier, uint tag, string name, uint flag, ref uint value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitInt64(IVisitier visitier, uint tag, string name, uint flag, ref long value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitUInt64(IVisitier visitier, uint tag, string name, uint flag, ref ulong value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitFloat(IVisitier visitier, uint tag, string name, uint flag, ref float value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitDouble(IVisitier visitier, uint tag, string name, uint flag, ref double value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitString(IVisitier visitier, uint tag, string name, uint flag, ref string value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitBooleanArray(IVisitier visitier, uint tag, string name, uint flag, ref bool[] value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitByteArray(IVisitier visitier, uint tag, string name, uint flag, ref byte[] value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
        private static void VisitSByteArray(IVisitier visitier, uint tag, string name, uint flag, ref sbyte[] value)
        {
            visitier.Visit(tag, name, flag, ref value);
        }
    }
}
