using UnityEditor;
using UnityEngine;

namespace LiteAnim.EditorView
{
    public static class LitAnimEditorUtil
    {
        public static void RegisterUndo(LiteAnimAsset asset, string actionName)
        {
            Undo.RegisterCompleteObjectUndo(asset, actionName);
            EditorUtility.SetDirty(asset);
        }

        public static void RegisterUndo(LiteAnimMotion motion, string actionName)
        {
            Undo.RegisterCompleteObjectUndo(motion, actionName);
            string path = AssetDatabase.GetAssetPath(motion);
            var asset = AssetDatabase.LoadAssetAtPath<LiteAnimAsset>(path);
            RegisterUndo(asset, actionName);
        }

        public static bool IsValidMotionName(LiteAnimAsset asset, string motionName)
        {
            if (string.IsNullOrEmpty(motionName))
                return false;
            foreach (var motion in asset.Motions)
            {
                if (motion != null && motion.name == motionName)
                    return false;
            }
            return true;
        }

        public static LiteAnimMotion CreateMotion(LiteAnimAsset asset, string motionName)
        {
            var motion = ScriptableObject.CreateInstance<LiteAnimMotion>();
            motion.name = motionName;
            string actionName = $"Create Motion '{motionName}'";
            Undo.RegisterCreatedObjectUndo(motion, actionName);
            asset.Motions.Add(motion);
            AssetDatabase.AddObjectToAsset(motion, asset);
            RegisterUndo(asset, actionName);
            return motion;
        }

        public static void DestroyMotion(LiteAnimAsset asset, LiteAnimMotion motion)
        {
            string actionName = $"Delete Motion '{motion.name}'";
            RegisterUndo(asset, actionName);
            Object.DestroyImmediate(motion, true);
            asset.Motions.Remove(motion);
        }
    }
}
