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

    public class SevenBitBase
    {
        public const uint RequiredFlag = 1;
        public const uint UnRequiredFlag = ~RequiredFlag;

        public static bool IsRequired(uint flag)
        {
            return (flag & RequiredFlag) != 0;
        }
    }
}
