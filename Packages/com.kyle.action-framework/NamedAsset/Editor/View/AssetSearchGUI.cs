using UnityEditor;
using UnityEngine;

namespace NamedAsset.Editor
{
    public static class AssetSearchGUI
    {
        public static string Field(Rect position, string val)
        {
            Rect btnRect = position;
            position.width -= 30;
            btnRect.x = position.xMax;
            btnRect.width = 30;

            val = EditorGUI.TextField(position, val);
            if (GUI.Button(btnRect, EditorGUIUtility.TrIconContent("d_SearchOverlay")))
            {
                NamedAssetSelectWindow.Show(position, val);
            }

            return NamedAssetSelectWindow.GetSelectKey(val, position);
        }
        public static string Field(Rect position, GUIContent label, string val)
        {
            Rect btnRect = position;
            position.width -= 30;
            btnRect.x = position.xMax;
            btnRect.width = 30;

            val = EditorGUI.TextField(position, label, val);
            if (GUI.Button(btnRect, EditorGUIUtility.TrIconContent("d_SearchOverlay")))
            {
                NamedAssetSelectWindow.Show(position, val);
            }

            return NamedAssetSelectWindow.GetSelectKey(val, position);
        }

        public static string LayoutPop(string val, params GUILayoutOption[] options)
        {
            var position = EditorGUILayout.GetControlRect(false, 18f, EditorStyles.popup, options);
            return Field(position, val);
        }
        public static string LayoutPop(GUIContent label, string val, params GUILayoutOption[] options)
        {
            var position = EditorGUILayout.GetControlRect(false, 18f, EditorStyles.popup, options);
            return Field(position, label, val);
        }
    }
}
