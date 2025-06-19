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
        private ActionLineAsset source;//继承的Source Asset，如果是变体，则指向原始Asset
        [SerializeField, ReadOnly]
        private List<ActionLineClip> clips = new List<ActionLineClip>();
        //禁用的Source Clips，仅变体生效
        [SerializeField, ReadOnly]
        private List<ActionLineClip> disableClips = new List<ActionLineClip>();
        //启用的Source Clips，仅变体生效,如果在Source中被禁用，则会在变体中启用
        [SerializeField, ReadOnly]
        private List<ActionLineClip> enableClips = new List<ActionLineClip>();
        [DisplayName("帧数"), ReadOnly]
        private int frameCount = 1;
        public int FrameCount=> (frameCount > 0 || !source) ? frameCount : source.frameCount;
        public int SelfFrameCount => frameCount;
        public bool IsVariant => source != null;
        public IReadOnlyList<ActionLineClip> Clips => clips;

        public ActionLineAsset Source => source;

        public void SetFrameCount(int count)
        {
            if(!source)
                frameCount = Mathf.Max(1, count);
            frameCount = count;
            EditorUtility.SetDirty(this);
        }

        public void SetSource(ActionLineAsset newSource)
        {
            if (newSource == this)
                return;
            if(newSource == null)
            {
                disableClips.Clear();
                enableClips.Clear();
            }
            else
            {
                disableClips.RemoveAll(clip => !newSource.ContainsClip(clip));
                enableClips.RemoveAll(clip => !newSource.ContainsClip(clip));
            }
            source = newSource;
            EditorUtility.SetDirty(this);
        }

        public void SetClipActive(ActionLineClip clip, bool active)
        {
            if(clip.Owner != this)
            {
                if(active)
                {
                    if (disableClips.Contains(clip))
                    {
                        disableClips.Remove(clip);
                    }
                    else if (!IsClipActive(clip))
                    {
                        enableClips.Add(clip);
                    }
                }
                else
                {
                    if (enableClips.Contains(clip))
                    {
                        enableClips.Remove(clip);
                    }
                    else if (IsClipActive(clip))
                    {
                        disableClips.Add(clip);
                    }
                }
            }
            clip.Disable = !active;
            EditorUtility.SetDirty(clip);
        }

        public bool ContainsClip(ActionLineClip clip)
        {
            if (clip == null)
                return false;
            if (clips.Contains(clip))
                return true;
            if (source)
                return source.ContainsClip(clip);
            return false;
        }

        public void ExportClipData(List<ActionClipData> datas)
        {
            if(source)
            {
                source.ExportClipData(datas);
                for (int i = 0; i < datas.Count; i++)
                {
                    var data = datas[i];
                    data.IsInherit = true;
                    data.IsActive = IsClipActive(data.Clip);
                    datas[i] = data;
                }
            }
            foreach (var clip in clips)
            {
                if (clip == null)
                    continue;
                ActionClipData data = new ActionClipData
                {
                    Clip = clip,
                    IsInherit = false,
                    IsActive = !clip.Disable
                };
                datas.Add(data);
            }
        }

        public bool IsClipActive(ActionLineClip clip)
        {
            if(clip.Owner == this)
                return !clip.Disable;
            if (disableClips.Contains(clip))
                return false;
            if (enableClips.Contains(clip))
                return true;
            if (!source)
                return source.IsClipActive(clip);
            return false;
        }

        public int AddClip(ActionLineClip clip)
        {
            if (clip == null) 
                return -1;
            int index = clips.IndexOf(clip);
            if (index >= 0)
            {
                return index;
            }
            clip.Owner = this;
            clips.Add(clip);
            AssetDatabase.RemoveObjectFromAsset(clip);
            AssetDatabase.AddObjectToAsset(clip, this);
            EditorUtility.SetDirty(this);
            return clips.Count - 1;
        }

        public void RemoveClip(ActionLineClip clip)
        {
            if (clip == null || clip.Owner != this || !clips.Contains(clip))
                return;
            clips.Remove(clip);
            //AssetDatabase.RemoveObjectFromAsset(clip);
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
