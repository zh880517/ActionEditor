using System;
using UnityEditor;

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
}
