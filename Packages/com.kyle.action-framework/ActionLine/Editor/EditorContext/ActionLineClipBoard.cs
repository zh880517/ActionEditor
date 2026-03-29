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
    public class ActionLineCopyData
    {
        public MonoScript AssetType;
        public List<ActionClipCopyData> Clips = new List<ActionClipCopyData>();
    }

    public class ActionLineClipBoard : ScriptableSingleton<ActionLineClipBoard>
    {
        [SerializeField]
        private List<ActionLineCopyData> datas = new List<ActionLineCopyData>();

        public static void AddCopyData(ActionLineCopyData data)
        {
            if(data == null || data.Clips.Count == 0 || data.AssetType == null)
                return;
            instance.datas.RemoveAll(it => it.AssetType == data.AssetType);
            instance.datas.Add(data);
        }

        public static ActionLineCopyData GetCopyData(MonoScript type)
        {
            if (type == null)
                return null;
            return instance.datas.Find(it => it.AssetType == type);
        }

        public static bool HasCopyData(MonoScript type)
        {
            return instance.datas.Exists(it => it.AssetType == type);
        }
    }
}
