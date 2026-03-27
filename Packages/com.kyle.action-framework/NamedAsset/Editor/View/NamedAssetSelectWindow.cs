using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NamedAsset.Editor
{
    internal class NamedAssetSelectWindow : ScriptableSingleton<NamedAssetSelectWindow>, ISearchWindowProvider
    {
        [SerializeField]
        private int version = 0;
        [SerializeField]
        private List<SearchTreeEntry> entries = new List<SearchTreeEntry>();
        private System.Action<string> onSelectAsset;
        private static int controlID = 0;
        private static string selectAsset = null;
        private static System.Type assetLimitType;
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (version != AssetCollector.instance.Version)
            {
                version = AssetCollector.instance.Version;
                entries.Clear();
                entries.Add(new SearchTreeGroupEntry(new GUIContent("选择资源"))
                { 
                    level = 0,
                });
                var packages = AssetCollector.instance.Packages;
                for (int i = 0; i < packages.Count; ++i)
                {
                    var package = packages[i];
                    var packageEntry = new SearchTreeGroupEntry(new GUIContent(package.Name))
                    {
                        level = 1,
                    };
                    entries.Add(packageEntry);
                    int avoidCount = 0;
                    for (int j = 0; j < package.Assets.Count; ++j)
                    {
                        var asset = package.Assets[j];
                        var type = AssetDatabase.GetMainAssetTypeAtPath(asset.Path);
                        if (assetLimitType != null && !assetLimitType.IsAssignableFrom(type))
                            continue;
                        avoidCount++;
                        var icon = AssetPreview.GetMiniTypeThumbnail(type);
                        string name = asset.Name.Substring(asset.Name.IndexOf('/') + 1);
                        var assetEntry = new SearchTreeEntry(new GUIContent(name, icon))
                        {
                            level = 2,
                            userData = asset.Name
                        };
                        entries.Add(assetEntry);
                    }
                    if (avoidCount == 0)
                    {
                        entries.RemoveAt(entries.Count-1);
                    }
                }
            }
            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            string key = searchTreeEntry.userData as string;
            if (onSelectAsset != null)
            {
                onSelectAsset(key);
                onSelectAsset = null;
            }
            else
            {
                selectAsset = key;
            }
            return true;
        }

        public static void Show(Vector2 position, System.Type assetType = null, System.Action<string> onSelect = null, float width = 0, float height = 0)
        {
            instance.onSelectAsset = onSelect;
            if (assetType != assetLimitType)
            {
                assetLimitType = assetType;
                instance.version = 0;
            }
            SearchWindow.Open(new SearchWindowContext(position, width, height), instance);
        }
        public static void Show(Rect activatorRect, string selected, float width = 0, float height = 0)
        {
            controlID = GetControlID(activatorRect);
            selectAsset = selected;
            Vector2 pos = activatorRect.position;
            pos.y += activatorRect.height;
            SearchWindow.Open(new SearchWindowContext(pos, width, height), instance);
        }

        private static int s_PopupHash = "NamedAssetSelectWindow".GetHashCode();
        private static int GetControlID(Rect activatorRect)
        {
            return GUIUtility.GetControlID(s_PopupHash, FocusType.Keyboard, activatorRect);
        }

        public static string GetSelectKey(string val, Rect activatorRect)
        {
            if (GetControlID(activatorRect) == controlID)
            {
                if (selectAsset != val)
                {
                    GUI.changed = true;
                    val = selectAsset;
                }
                controlID = 0;
                selectAsset = null;
            }
            return val;
        }
    }
}
