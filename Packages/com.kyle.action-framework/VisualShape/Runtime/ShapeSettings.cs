#pragma warning disable CS0169, CS0414 // The field 'ShapeSettings.version' is never used
using UnityEditor;
using UnityEngine;

namespace VisualShape
{
    /// <summary>存储 VisualShape 项目设置</summary>
    public class ShapeSettings : ScriptableObject
    {
        public const string SettingsPathCompatibility = "Assets/Settings/VisualShape.asset";
        public const string SettingsName = "VisualShape";
        public const string SettingsPath = "Assets/Settings/Resources/" + SettingsName + ".asset";

        /// <summary>存储 VisualShape 项目设置</summary>
        [System.Serializable]
        public class Settings
        {
            /// <summary>线条在物体前方时的不透明度</summary>
            public float lineOpacity = 1.0f;

            /// <summary>实体在其他物体前方时的不透明度</summary>
            public float solidOpacity = 0.55f;

            /// <summary>文本在其他物体前方时的不透明度</summary>
            public float textOpacity = 1.0f;

            /// <summary>线条在物体后方或内部时的附加不透明度乘数</summary>
            public float lineOpacityBehindObjects = 0.12f;

            /// <summary>实体在其他物体后方或内部时的附加不透明度乘数</summary>
            public float solidOpacityBehindObjects = 0.45f;

            /// <summary>文本在其他物体后方或内部时的附加不透明度乘数</summary>
            public float textOpacityBehindObjects = 0.9f;

            /// <summary>
            /// 曲线分辨率，以默认值的倍数表示。
            ///
            /// 曲线分辨率会根据到相机的距离动态调整。
            /// 此设置将基于默认值按倍数提高或降低曲线分辨率。
            /// </summary>
            public float curveResolution = 1.0f;
        }

        [SerializeField]
        private int version;
        public Settings settings;

        public static Settings DefaultSettings => new Settings();

        public static ShapeSettings GetSettingsAsset()
        {
#if UNITY_EDITOR
            System.IO.Directory.CreateDirectory(Application.dataPath + "/../" + System.IO.Path.GetDirectoryName(SettingsPath));
            var settings = AssetDatabase.LoadAssetAtPath<ShapeSettings>(SettingsPath);
            if (settings == null && AssetDatabase.LoadAssetAtPath<ShapeSettings>(SettingsPathCompatibility) != null)
            {
                AssetDatabase.MoveAsset(SettingsPathCompatibility, SettingsPath);
                settings = AssetDatabase.LoadAssetAtPath<ShapeSettings>(SettingsPath);
            }
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ShapeSettings>();
                settings.settings = DefaultSettings;
                settings.version = 0;
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
#else
            var settings = Resources.Load<ShapeSettings>(SettingsName);
            if (settings == null)
            {
                Debug.LogWarning($"VisualShape: Could not load settings asset from Resources/{SettingsName}. Using default settings.");
            }
#endif
            return settings;
        }
    }
}
