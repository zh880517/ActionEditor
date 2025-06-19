using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionLine.EditorView
{
    public static class ActionClipTypeUtil
    {
        public class ClipTypeInfo
        {
            public string Name;
            public Color ClipColor;
            public Texture Icon;
        }

        private static Dictionary<Type, ClipTypeInfo> clipTypeInfos = new Dictionary<Type, ClipTypeInfo>();

        public static ClipTypeInfo GetTypeInfo<T>() where T : ScriptableObject
        {
            return GetTypeInfo(typeof(T));
        }

        public static ClipTypeInfo GetTypeInfo(Type type)
        {
            if (!clipTypeInfos.TryGetValue(type, out var info))
            {
                info = new ClipTypeInfo
                {
                    Name = type.Name,
                    ClipColor = Color.white,
                    Icon = null,
                };
                var colorAttribute = type.GetCustomAttribute<ActionClipColorAttribute>();
                if (colorAttribute != null)
                {
                    info.ClipColor = colorAttribute.ClipColor;
                }
                var displayNameAttribute = type.GetCustomAttribute<AliasAttribute>();
                if (displayNameAttribute != null)
                {
                    info.Name = displayNameAttribute.Name;
                }
                info.Icon = MonoScriptUtil.GetTypeIcon(type);
                clipTypeInfos.Add(type, info);
            }
            return info;
        }
    }
}
