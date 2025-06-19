using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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


        public static void CollectEditorAction(Type assetType, List<EditorAction> actions)
        {
            var types = TypeCollector<EditorAction>.Types;
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<CustomEditorActionAttribute>(true);
                if (attributes.Count() == 0)
                {
                    // 如果没有自定义属性，则认为该类型的 EditorAction 可以应用于所有类型的 Asset
                    EditorAction action = (EditorAction)Activator.CreateInstance(type);
                    actions.Add(action);
                }
                else
                {
                    foreach (var item in attributes)
                    {
                        if (item.AssetType.IsAssignableFrom(assetType))
                        {
                            EditorAction action = (EditorAction)Activator.CreateInstance(type);
                            actions.Add(action);
                            break;
                        }
                    }
                }
            }
            actions.Sort((x, y) => x.ShowOrder.CompareTo(y.ShowOrder));
        }
    }
}
