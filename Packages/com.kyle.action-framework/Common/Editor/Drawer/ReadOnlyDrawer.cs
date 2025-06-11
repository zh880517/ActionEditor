using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new  EditorGUI.DisabledScope(false))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var propertyField = new PropertyField(property);
        propertyField.SetEnabled(false); // 禁用编辑
        return propertyField;
    }
}