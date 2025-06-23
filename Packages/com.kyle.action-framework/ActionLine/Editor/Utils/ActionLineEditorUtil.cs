using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionLine.EditorView
{
    public static class ActionLineEditorUtil
    {
        private readonly static List<int> indexsCache = new List<int>();

        public static T CreateClip<T>(string undoName = null) where T : ActionLineClip
        {
            return CreateClip(typeof(T), undoName) as T;
        }

        public static ActionLineClip CreateClip(Type type, string undoName = null)
        {
            if (type == null || !typeof(ActionLineClip).IsAssignableFrom(type))
                throw new ArgumentException("Type must be a subclass of ActionLineClip", nameof(type));
            var clip = (ActionLineClip)ScriptableObject.CreateInstance(type);
            clip.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            var info = ActionClipTypeUtil.GetTypeInfo(type);
            clip.name = info.Name;
            if (undoName != null)
                Undo.RegisterCreatedObjectUndo(clip, "Create ActionLine Clip");
            return clip;
        }

        public static ActionLineCopyDate Copy(ActionLineEditorContext context, bool addToClipBoard = true)
        {
            if (context.SelectedClips.Count == 0 || context.SelectedTracks.Count == 0)
            {
                return null;
            }
            ActionLineCopyDate copyData = new ActionLineCopyDate();
            copyData.AssetType = MonoScript.FromScriptableObject(context.Target);
            indexsCache.Clear();
            for (int i = 0; i < context.SelectedClips.Count; i++)
            {
                ActionClipData data = context.SelectedClips[i];
                int index = context.GetIndex(data);
                if (index >= 0 && !indexsCache.Contains(index))
                {
                    indexsCache.Add(index);
                }
            }
            for (int i = 0; i < context.SelectedTracks.Count; i++)
            {
                ActionClipData data = context.SelectedTracks[i];
                int index = context.GetIndex(data);
                if (index >= 0 && !indexsCache.Contains(index))
                {
                    indexsCache.Add(index);
                }
            }
            indexsCache.Sort();
            foreach (var idx in indexsCache)
            {
                var clipData = context.Clips[idx];
                if (clipData == null || clipData.Clip == null)
                    continue;
                ActionClipCopyData data = new ActionClipCopyData
                {
                    Type = MonoScript.FromScriptableObject(clipData.Clip),
                    Data = EditorJsonUtility.ToJson(clipData.Clip),
                };
                copyData.Clips.Add(data);
            }
            if (addToClipBoard)
            {
                ActionLineClipBoard.AddCopyData(copyData);
            }
            return copyData;
        }

        public static void Paste(ActionLineEditorContext context, ActionLineCopyDate copyData)
        {
            if (copyData == null)
                return;
            //清理剪切板中被删除的 Clip
            copyData.Clips.RemoveAll(it => !it.Type);
            if (copyData.Clips.Count == 0)
                return;
            if (MonoScript.FromScriptableObject(context.Target) != copyData.AssetType)
                return;
            context.RegisterUndo("Paste ActionLine Clips");
            indexsCache.Clear();
            foreach (var item in copyData.Clips)
            {
                var type = item.Type.GetClass();
                var clip = CreateClip(type, "Paste ActionLine Clip");
                if (clip == null)
                {
                    Debug.LogError($"Failed to create clip of type {type.Name}");
                    continue;
                }
                EditorJsonUtility.FromJsonOverwrite(item.Data, clip);
                clip.Owner = null;
                int index = context.Target.AddClip(clip);
                indexsCache.Add(index);
            }
            context.SelectedClips.Clear();
            context.SelectedTracks.Clear();
            foreach (var idx in indexsCache)
            {
                var clip = context.Target.Clips[idx];
                var data = context.Clips.First(it => it.Clip == clip);
                context.SelectedTracks.Add(data);
                context.SelectedClips.Add(data);
            }

            context.RefreshView();
        }

        public static void PasteFromClipboard(ActionLineEditorContext context)
        {
            var monoScript = MonoScript.FromScriptableObject(context.Target);
            var copyData = ActionLineClipBoard.GetCopyData(monoScript);
            if (copyData == null)
            {
                return;
            }
            Paste(context, copyData);
        }

        public static void Dumplicate(ActionLineEditorContext context)
        {
            var copyData = Copy(context, false);
            if(copyData != null)
            {
                Paste(context, copyData);
            }
        }

        public static void DeleteSelectedClips(ActionLineEditorContext context, bool clip, bool track)
        {
            if (context.SelectedClips.Count == 0 || context.SelectedTracks.Count == 0)
                return;
            indexsCache.Clear();
            foreach(var clipData in context.SelectedClips)
            {
                if(clipData.IsInherit)
                    continue; //不支持删除继承的 Clip
                int index = context.GetIndex(clipData);
                if (index >= 0 && !indexsCache.Contains(index))
                {
                    indexsCache.Add(index);
                }
            }
            foreach (var trackData in context.SelectedTracks)
            {
                if (trackData.IsInherit)
                    continue; //不支持删除继承的 Clip
                int index = context.GetIndex(trackData);
                if (index >= 0 && !indexsCache.Contains(index))
                {
                    indexsCache.Add(index);
                }
            }
            indexsCache.Sort();
            if (indexsCache.Count == 0)
                return;
            context.RegisterUndo("Delete ActionLine Clips");
            for (int i = indexsCache.Count - 1; i >= 0; i--)
            {
                int index = indexsCache[i];
                if (index < 0 || index >= context.Clips.Count)
                    continue;
                var clipData = context.Clips[index];
                context.Target.RemoveClip(clipData.Clip);
                Undo.DestroyObjectImmediate(clipData.Clip);
            }
            context.SelectedClips.Clear();
            context.SelectedTracks.Clear();
            context.RefreshView();
        }

    }
}
