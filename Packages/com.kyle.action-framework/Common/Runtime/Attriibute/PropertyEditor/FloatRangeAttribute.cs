using PropertyEditor;

public class FloatRangeAttribute : CustomPropertyAttribute
{
    public float Min { get; private set; }
    public float Max { get; private set; }
    public FloatRangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
