using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
[CustomPropertyDrawer(typeof(DisplayNameAttribute))]
internal class DisplayNameDrawer : PropertyDrawer
{
    // 传统的 OnGUI 方式（可选）
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DisplayNameAttribute displayName = attribute as DisplayNameAttribute;
        label.text = displayName.DisplayName;
        label.tooltip = displayName.Tooltip;
        EditorGUI.PropertyField(position, property, label);
    }

    // 使用 CreatePropertyGUI（UI Toolkit 方式）
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        DisplayNameAttribute displayName = attribute as DisplayNameAttribute;

        // 创建一个 PropertyField，并修改其 label
        var propertyField = new PropertyField(property, displayName.DisplayName);
        propertyField.tooltip = displayName.Tooltip;
        return propertyField;
    }
}
