using PropertyEditor;

public class StringPopupSelectAttribute : CustomPropertyAttribute
{
    public string[] Options { get; private set; }
    public StringPopupSelectAttribute(string[] options)
    {
        Options = options;
    }
}
