using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class MonoScriptUtil
{
    public static MonoScript GetMonoScript<T>() where T : UnityEngine.Object
    {
        return GetMonoScript(typeof(T));
    }

    public static MonoScript GetMonoScript(Type type)
    {
        var t = typeof(MonoScript);
        var method = t.GetMethod("FromType", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (method != null)
        {
            return method.Invoke(t, new object[] { type }) as MonoScript;
        }
        return null;
    }

    public static Texture2D GetMonoScriptIcon(MonoScript script)
    {
        var import = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(script)) as MonoImporter;
        if (import != null)
        {
            var icon = import.GetIcon();
            if (icon != null)
                return icon;
        }
        return EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
    }


    public static Texture2D GetMonoScriptIcon<T>() where T : UnityEngine.Object
    {
        return GetMonoScriptIcon(GetMonoScript<T>());
    }

    public static Texture2D GetTypeIcon(Type type)
    {
        if(type.IsSubclassOf(typeof(ScriptableObject)))
        {
            var script = GetMonoScript(type);
            return GetMonoScriptIcon(script);
        }
        return AssetPreview.GetMiniTypeThumbnail(type);
    }
}
