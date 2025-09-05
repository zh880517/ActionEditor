using PropertyEditor;

public class IntegerRangeAttribute : CustomPropertyAttribute
{
    public int Min { get; private set; }
    public int Max { get; private set; }
    public IntegerRangeAttribute(int min, int max)
    {
        Min = min;
        Max = max;
    }
}
