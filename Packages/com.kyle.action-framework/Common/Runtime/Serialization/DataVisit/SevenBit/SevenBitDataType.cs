namespace DataVisit
{
    public enum SevenBitDataType
    {
        Positive = 0,
        Negative = 1,
        Float = 2,
        Double = 3,
        String = 4,
        Vector = 5,
        StructBegin = 6,
        DynamicBegin = 7,
        StructEnd = 8,
    }

    public struct Header
    {
        public uint tag;
        public SevenBitDataType type;
    }
}
