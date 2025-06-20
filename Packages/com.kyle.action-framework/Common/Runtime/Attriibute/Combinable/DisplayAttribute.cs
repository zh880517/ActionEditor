public class DisplayAttribute : CombinableProertyAttribute
{
    public string Name { get; private set; }
    public string Tooltip { get; private set; }

    public DisplayAttribute(string name, string tooltip = null)
    {
        Name = name;
        Tooltip = tooltip;
    }
}
