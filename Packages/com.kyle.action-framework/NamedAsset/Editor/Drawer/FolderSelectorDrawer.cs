using UnityEditor;
using UnityEngine;

namespace NamedAsset.Editor
{
    [CustomPropertyDrawer(typeof(FolderSelectorAttribute))]
    internal class FolderSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect btnRect = position;
            position.width -= 30;
            btnRect.x = position.xMax;
            btnRect.width = 30;
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, true);
            if (GUI.Button(btnRect, EditorGUIUtility.TrIconContent("d_FolderOpened Icon")))
            {
                string value = property.stringValue;
                if (string.IsNullOrEmpty(value))
                {
                    value = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel("选择文件夹", value, "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Replace('\\', '/');
                    int index = path.IndexOf("Assets/");
                    if (index >= 0)
                    {
                        path = path.Substring(index);
                    }
                    else
                    {
                        index = path.IndexOf("Packages/");
                        if (index >= 0)
                        {
                            path = path.Substring(index);
                        }
                    }
                    property.stringValue = path;
                }
            }
            EditorGUI.EndProperty();
        }
    }
}
