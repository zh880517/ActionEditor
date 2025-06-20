using UnityEditor;
using UnityEditor.UIElements;

internal class DisplayDrawer : TCombinableDrawer<DisplayAttribute>
{
    protected override void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, DisplayAttribute attribute)
    {
        field.label = attribute.Name;
        field.tooltip = attribute.Tooltip;
    }
}
