using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ScriptObjectCollector : ScriptableSingleton<ScriptObjectCollector>
{
    public static bool HasInstance { get; private set; }

    [System.Serializable]
    class ObjectInfo
    {
        public MonoScript Script;
        public List<string> Fiels = new List<string>();
    }
    [SerializeField]
    private List<ObjectInfo> objects = new List<ObjectInfo>();
    private List<MonoScript> modifyAssetTypes = new List<MonoScript>();

    public static event System.Action<MonoScript> OnAssetChanged;

    public IReadOnlyList<string> GetAssets(MonoScript script)
    {
        var info = instance.objects.Find(o => o.Script == script);
        if (info != null)
            return info.Fiels;
        return null;
    }

    private void Awake()
    {
        var files = System.IO.Directory.GetFiles("Assets/", "*.asset", System.IO.SearchOption.AllDirectories);
        foreach (var item in files)
        {
            string path = item.Replace("\\", "/");
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            var mono = MonoScriptUtil.GetMonoScript(type);
            if (mono != null)
                AddAsset(mono, path);
        }
    }
    private void AddAsset(MonoScript script, string path)
    {
        var info = objects.Find(o => o.Script == script);
        if (info == null)
        {
            info = new ObjectInfo() { Script = script };
            objects.Add(info);
        }
        if (!info.Fiels.Contains(path))
        {
            info.Fiels.Add(path);
        }
    }

    public void OnAssetChange(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var item in importedAssets)
        {
            if (!item.EndsWith(".asset"))
                continue;
            var type = AssetDatabase.GetMainAssetTypeAtPath(item);
            var mono = MonoScriptUtil.GetMonoScript(type);
            if (mono != null)
            {
                AddAsset(mono, item);
                if (!modifyAssetTypes.Contains(mono))
                    modifyAssetTypes.Add(mono);
            }
        }
        foreach (var item in deletedAssets)
        {
            if (!item.EndsWith(".asset"))
                continue;
            foreach (var res in objects)
            {
                if(res.Fiels.Contains(item))
                {
                    res.Fiels.Remove(item);
                    if (!modifyAssetTypes.Contains(res.Script))
                        modifyAssetTypes.Add(res.Script);
                    break;
                }
            }
        }
        for (int i = 0; i < movedAssets.Length; i++)
        {
            var from = movedFromAssetPaths[i];
            var to = movedAssets[i];
            if (!from.EndsWith(".asset") || !to.EndsWith(".asset"))
                continue;
            var type = AssetDatabase.GetMainAssetTypeAtPath(to);
            var mono = MonoScriptUtil.GetMonoScript(type);
            if (mono == null)
                continue;
            var info = objects.Find(o => o.Script == mono);
            if (info == null)
            {
                info = new ObjectInfo() { Script = mono };
                objects.Add(info);
            }
            if (!info.Fiels.Contains(to))
            {
                info.Fiels.Add(to);
            }
            info.Fiels.Remove(from);
            if (!modifyAssetTypes.Contains(mono))
                modifyAssetTypes.Add(mono);
        }
        foreach (var item in modifyAssetTypes)
        {
            OnAssetChanged?.Invoke(item);
        }
        modifyAssetTypes.Clear();
    }

    private void OnEnable()
    {
        HasInstance = true;
        objects.RemoveAll(o => o.Script == null);
    }
    private void OnDestroy()
    {
        HasInstance = false;
    }
}