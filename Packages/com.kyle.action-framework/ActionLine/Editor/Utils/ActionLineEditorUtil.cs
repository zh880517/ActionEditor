using System;
using UnityEditor;
using UnityEngine;

namespace ActionLine.EditorView
{
    public static class ActionLineEditorUtil
    {
        public static T CreateClip<T>(string undoName = null) where T :ActionLineClip
        {
            return CreateClip(typeof(T), undoName) as T;
        }

        public static ActionLineClip CreateClip(Type type, string undoName = null)
        {
            if (type == null || !typeof(ActionLineClip).IsAssignableFrom(type))
                throw new ArgumentException("Type must be a subclass of ActionLineClip", nameof(type));
            var clip = (ActionLineClip)ScriptableObject.CreateInstance(type);
            clip.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            clip.name = type.Name;
            if (undoName != null)
                Undo.RegisterCreatedObjectUndo(clip, "Create ActionLine Clip");
            return clip;
        }
    }
}
