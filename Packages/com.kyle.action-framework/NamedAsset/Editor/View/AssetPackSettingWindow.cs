using System.Collections.Generic;
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

        // ── 页签 ──────────────────────────────────────────────────────────
        private int m_TabIndex;
        private readonly string[] k_TabNames = { "打包设置", "资源浏览器" };

        // ── 打包设置页 ────────────────────────────────────────────────────
        public bool ForceBundle;
        public UnityEditor.Editor settingEditor;
        public Vector2 scrollPos;

        // ── 资源浏览器页 ──────────────────────────────────────────────────
        private string m_SearchText = "";
        private Vector2 m_BrowserScroll;

        // 缓存：展开状态；key = 包名
        private readonly Dictionary<string, bool> m_PackageFoldout = new Dictionary<string, bool>();

        // 重名资源名集合（跨包）
        private HashSet<string> m_DuplicateNames;
        private int m_LastCollectorVersion = -1;

        private GUIStyle m_DuplicateStyle;
        private GUIStyle m_NormalNameStyle;

        // ─────────────────────────────────────────────────────────────────

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
            m_TabIndex = GUILayout.Toolbar(m_TabIndex, k_TabNames);
            GUILayout.Space(4);

            if (m_TabIndex == 0)
                DrawSettingsTab();
            else
                DrawBrowserTab();
        }

        // ── 打包设置页 ────────────────────────────────────────────────────

        private void DrawSettingsTab()
        {
            using (var scroll = new GUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scroll.scrollPosition;
                settingEditor.OnInspectorGUI();
            }
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.HorizontalScope())
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

        // ── 资源浏览器页 ──────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (m_DuplicateStyle == null)
            {
                m_DuplicateStyle = new GUIStyle(EditorStyles.label);
                m_DuplicateStyle.normal.textColor = new Color(0.95f, 0.25f, 0.25f);
            }
            if (m_NormalNameStyle == null)
            {
                m_NormalNameStyle = new GUIStyle(EditorStyles.label);
            }
        }

        private void RebuildDuplicateSet(List<AssetCollector.Package> packages)
        {
            var seen = new HashSet<string>();
            m_DuplicateNames = new HashSet<string>();
            foreach (var pkg in packages)
            {
                foreach (var asset in pkg.Assets)
                {
                    if (!seen.Add(asset.Name))
                        m_DuplicateNames.Add(asset.Name);
                }
            }
        }

        private void DrawBrowserTab()
        {
            EnsureStyles();

            var collector = AssetCollector.instance;

            // 检测 collector 版本变化，重建重名集合
            if (collector.Version != m_LastCollectorVersion)
            {
                m_LastCollectorVersion = collector.Version;
                RebuildDuplicateSet(collector.Packages);
            }

            // ── 搜索栏 ──
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("搜索", GUILayout.Width(36));
                string newSearch = EditorGUILayout.TextField(m_SearchText, EditorStyles.toolbarSearchField);
                if (newSearch != m_SearchText)
                {
                    m_SearchText = newSearch;
                    // 搜索时展开所有命中的包
                    if (!string.IsNullOrEmpty(m_SearchText))
                    {
                        foreach (var pkg in collector.Packages)
                            m_PackageFoldout[pkg.Name] = true;
                    }
                }
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                    m_SearchText = "";

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(48)))
                {
                    collector.ForceRefresh();
                    m_LastCollectorVersion = -1;
                    Repaint();
                }
            }

            string filter = m_SearchText.ToLower();

            using (var scroll = new GUILayout.ScrollViewScope(m_BrowserScroll))
            {
                m_BrowserScroll = scroll.scrollPosition;

                foreach (var pkg in collector.Packages)
                {
                    // 过滤：包名或任意资源名命中
                    bool packageNameHit = string.IsNullOrEmpty(filter) || pkg.Name.ToLower().Contains(filter);
                    List<AssetCollector.AssetRef> visibleAssets = new List<AssetCollector.AssetRef>();
                    foreach (var asset in pkg.Assets)
                    {
                        if (string.IsNullOrEmpty(filter)
                            || asset.Name.ToLower().Contains(filter)
                            || packageNameHit)
                        {
                            visibleAssets.Add(asset);
                        }
                    }

                    if (visibleAssets.Count == 0)
                        continue;

                    // ── 包折叠头 ──
                    if (!m_PackageFoldout.ContainsKey(pkg.Name))
                        m_PackageFoldout[pkg.Name] = true;

                    using (new GUILayout.HorizontalScope())
                    {
                        m_PackageFoldout[pkg.Name] = EditorGUILayout.Foldout(
                            m_PackageFoldout[pkg.Name],
                            $"{pkg.Name}  ({visibleAssets.Count})",
                            true,
                            EditorStyles.foldoutHeader);
                    }

                    if (!m_PackageFoldout[pkg.Name])
                        continue;

                    // ── 资源列表 ──
                    foreach (var asset in visibleAssets)
                    {
                        bool isDup = m_DuplicateNames != null && m_DuplicateNames.Contains(asset.Name);
                        GUIStyle nameStyle = isDup ? m_DuplicateStyle : m_NormalNameStyle;

                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16);

                            // 资源名（重名标红）
                            string displayName = asset.Name;
                            GUIContent nameContent = isDup
                                ? new GUIContent($"⚠ {displayName}", "与其他包存在重名资源")
                                : new GUIContent(displayName);
                            GUILayout.Label(nameContent, nameStyle, GUILayout.ExpandWidth(true));

                            // 复制资源名按钮
                            if (GUILayout.Button("复制", GUILayout.Width(40)))
                            {
                                GUIUtility.systemCopyBuffer = asset.Name;
                                Debug.Log($"已复制: {asset.Name}");
                            }

                            // 定位资源按钮
                            if (GUILayout.Button("Select", GUILayout.Width(52)))
                            {
                                var obj = AssetDatabase.LoadMainAssetAtPath(asset.Path);
                                if (obj != null)
                                {
                                    EditorGUIUtility.PingObject(obj);
                                    Selection.activeObject = obj;
                                }
                                else
                                {
                                    Debug.LogWarning($"找不到资源: {asset.Path}");
                                }
                            }
                        }
                    }

                    GUILayout.Space(2);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────

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

