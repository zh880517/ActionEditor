using PropertyEditor;

public class IntPopupSelectAttribute : CustomPropertyAttribute
{
    public string[] Options { get; private set; }
    public IntPopupSelectAttribute(string[] options)
    {
        Options = options;
    }
}
