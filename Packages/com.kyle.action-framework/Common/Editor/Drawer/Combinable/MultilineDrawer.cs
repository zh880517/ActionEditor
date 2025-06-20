using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


internal class MultilineDrawer : TCombinableDrawer<MultilineAttribute>
{
    protected override void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, MultilineAttribute attribute)
    {
        var text = field.Q<TextField>();
        if(text != null)
        {
            text.multiline = true;
            text.style.whiteSpace = WhiteSpace.Normal;
        }
    }
}
