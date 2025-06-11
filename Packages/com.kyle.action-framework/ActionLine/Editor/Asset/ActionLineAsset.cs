using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ActionLine
{
    /* 
     * 创建时需要设置 hideFlags = HideFlags.DontSave，编辑时不会将场景设置为dirty
     */
    public class ActionLineAsset : ScriptableObject
    {
        [SerializeField, ReadOnly]
        private List<ActionLineClip> clips = new List<ActionLineClip>();
        [DisplayName("帧数"), ReadOnly]
        public int FrameCount;

        public IReadOnlyList<ActionLineClip> Clips => clips;

        public void AddClip(ActionLineClip clip)
        {
            if (clip == null) 
                return;
            int index = clips.IndexOf(clip);
            if (index >= 0)
            {
                return;
            }
            clips.Add(clip);
            AssetDatabase.RemoveObjectFromAsset(clip);
            AssetDatabase.AddObjectToAsset(clip, this);
            EditorUtility.SetDirty(this);
        }

        public void RemoveClip(ActionLineClip clip)
        {
            if (clip == null || !clips.Contains(clip))
                return;
            clips.Remove(clip);
            AssetDatabase.RemoveObjectFromAsset(clip);
            EditorUtility.SetDirty(this);
        }

        public int MoveToBehind(ActionLineClip clip, ActionLineClip target)
        {
            if (clip == null)
                return -1;
            int oldIndex = clips.IndexOf(clip);
            if (oldIndex < 0)
                return -1;
            if (clip == target)
                return oldIndex;

            int targetIndex = clips.IndexOf(target);
            if (targetIndex < 0)
            {
                clips.Insert(0, clip);
                return 0;
            }

            int newIndex = targetIndex + 1;
            if(oldIndex < targetIndex)
                newIndex--;
            clips.RemoveAt(oldIndex);
            clips.Insert(newIndex, clip);
            EditorUtility.SetDirty(this);
            return newIndex;
        }

        public void RegisterUndo(string name)
        {
            Undo.RegisterCompleteObjectUndo(this, name);
            EditorUtility.SetDirty(this);
        }
    }
}
