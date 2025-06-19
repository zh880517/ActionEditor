using System;
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
        var method = t.GetMethod("FromType", System.Reflection.BindingFlags.Static);
        if (method != null)
        {
            return method.Invoke(t, new object[] { type }) as MonoScript;
        }
        return null;
    }

    public static Texture2D GetMonoScriptIcon(MonoScript script)
    {
        return script != null ? AssetPreview.GetMiniThumbnail(script) : null;
    }

    public static Texture2D GetMonoScriptIcon<T>() where T : UnityEngine.Object
    {
        return GetMonoScriptIcon(GetMonoScript<T>());
    }

    public static Texture2D GetTypeIcon(Type type)
    {
        if (!type.IsSubclassOf(typeof(ScriptableObject)) && !type.IsSubclassOf(typeof(MonoBehaviour)))
            return null;
        MonoScript script = GetMonoScript(type);
        return GetMonoScriptIcon(script);
    }
}
