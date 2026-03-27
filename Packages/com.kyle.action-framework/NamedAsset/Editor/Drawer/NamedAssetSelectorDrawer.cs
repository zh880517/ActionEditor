using UnityEditor;
using UnityEngine;

namespace NamedAsset.Editor
{
    [CustomPropertyDrawer(typeof(NamedAssetSelectAttribute))]
    internal class NamedAssetSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect btnRect = position;
            position.width -= 30;
            btnRect.x = position.xMax;
            btnRect.width = 30;
            EditorGUI.BeginProperty(position, label, property);
            Color color = GUI.color;
            bool valid = !string.IsNullOrEmpty(property.stringValue) && AssetCollector.instance.GetAssetPath(property.stringValue) != null;
            if (!valid)
            {
                GUI.color = Color.red;
            }
            EditorGUI.PropertyField(position, property, true);
            GUI.color = color;
            if (GUI.Button(btnRect, EditorGUIUtility.TrIconContent("d_SearchOverlay")))
            {
                NamedAssetSelectWindow.Show(position, property.stringValue);
            }
            property.stringValue = NamedAssetSelectWindow.GetSelectKey(property.stringValue, position);
            EditorGUI.EndProperty();
        }
    }
}
