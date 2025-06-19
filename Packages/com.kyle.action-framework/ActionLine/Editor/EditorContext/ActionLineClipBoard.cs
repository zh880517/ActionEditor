using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ActionLine.EditorView
{
    [System.Serializable]
    public struct ActionClipCopyData
    {
        public MonoScript Type;
        public string Data;
    }

    [System.Serializable]
    public class ActionLineCopyDate
    {
        public MonoScript AssetType;
        public List<ActionClipCopyData> Clips = new List<ActionClipCopyData>();
    }

    public class ActionLineClipBoard : ScriptableSingleton<ActionLineClipBoard>
    {
        [SerializeField]
        private List<ActionLineCopyDate> dates = new List<ActionLineCopyDate>();

        public static void AddCopyData(ActionLineCopyDate data)
        {
            if(data == null || data.Clips.Count == 0 || data.AssetType == null)
                return;
            instance.dates.RemoveAll(it => it.AssetType == data.AssetType);
            instance.dates.Add(data);
        }

        public static ActionLineCopyDate GetCopyData(MonoScript type)
        {
            if (type == null)
                return null;
            return instance.dates.Find(it => it.AssetType == type);
        }

        public static bool HasCopyData(MonoScript type)
        {
            return instance.dates.Exists(it => it.AssetType == type);
        }
    }
}
