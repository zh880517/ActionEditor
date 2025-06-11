using UnityEngine;

public class DisplayNameAttribute : PropertyAttribute
{
    public string DisplayName { get; private set; }
    public string Tooltip { get; private set; }
    public DisplayNameAttribute(string displayName, string tooltip = "")
    {
        DisplayName = displayName;
        Tooltip = tooltip;
    }
}
