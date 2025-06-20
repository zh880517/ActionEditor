using UnityEditor;
using UnityEditor.UIElements;
internal class ReadOnlyDrawer : TCombinableDrawer<ReadOnlyAttribute>
{
    protected override void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, ReadOnlyAttribute attribute)
    {
        field.SetEnabled(false);
    }
}