using UnityEditor;
using UnityEngine;

namespace NamedAsset.Editor
{
    [EditorWindowTitle(title = "打包资源编辑器")]
    public class AssetPackSettingWindow : EditorWindow
    {
        [MenuItem("Window/打包资源编辑器")]
        public static void ShowWindow()
        {
            GetWindow<AssetPackSettingWindow>();
        }

        public bool ForceBundle;
        public UnityEditor.Editor settingEditor;
        public Vector2 scrollPos;
        private void OnEnable()
        {
            if (settingEditor == null)
            {
                settingEditor = UnityEditor.Editor.CreateEditor(AssetPackSetting.instance);
            }
            ForceBundle = EditorPrefs.GetBool("_forceBundle_");
        }

        private void OnGUI()
        {
            using(var scroll = new GUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scroll.scrollPosition;
                settingEditor.OnInspectorGUI();
            }
            EditorGUI.BeginChangeCheck();
            using(new GUILayout.HorizontalScope())
            {
                GUILayout.Label("编辑器模式使用AssetBundle方式加载");

                ForceBundle = EditorGUILayout.Toggle(ForceBundle);
                GUILayout.FlexibleSpace();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("_forceBundle_", ForceBundle);
            }
            if (GUILayout.Button("Build AssetBundle"))
            {
                AssetPackSetting.instance.Build();
            }
            GUILayout.Space(10);
        }


        private void OnDestroy()
        {
            if (settingEditor != null)
            {
                DestroyImmediate(settingEditor);
            }
            AssetPackSetting.instance.Save();
        }
    }
}
