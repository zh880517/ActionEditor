using UnityEditor;
using UnityEditor.UIElements;

internal class PropertyMotionDrawer : TCombinableDrawer<PropertyMotionAttribute>
{
    protected override void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, PropertyMotionAttribute attribute)
    {
        field.TrackPropertyValue(property, (p) =>
        {
            using (var evt = PropertyChangeEvent.GetPooled(p))
            {
                field.SendEvent(evt);
            }
        });
    }
}
