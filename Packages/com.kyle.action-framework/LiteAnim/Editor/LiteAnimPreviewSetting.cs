using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiteAnim.EditorView
{

    [FilePath("EditorUserSetting/LiteAnimPreviewSetting.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class LiteAnimPreviewSetting : ScriptableSingleton<LiteAnimPreviewSetting>
    {
        [System.Serializable]
        public struct PreviewBind
        {
            public LiteAnimAsset Asset;
            public GameObject Target;
        }
        public bool EnablePreview = false;
        private List<PreviewBind> binds = new List<PreviewBind>();

        public void AddBind(LiteAnimAsset asset, GameObject target)
        {
            
            int index = binds.FindIndex(it=>it.Asset == asset);
            if(index == -1)
            {
                if(target)
                {
                    binds.Add(new PreviewBind() { Asset = asset, Target = target });
                    Save(true);
                }
                return;
            }
            else
            {
                if(!target)
                {
                    binds.RemoveAt(index);
                    Save(true);
                }
                else
                {
                    binds[index] = new PreviewBind() { Asset = asset, Target = target };
                }
            }
        }

        public GameObject GetBindTarget(LiteAnimAsset asset)
        {
            for (int i = 0; i < binds.Count; i++)
            {
                var v = binds[i];
                if (v.Asset == asset)
                {
                    return v.Target;
                }
            }
            return null;
        }

        public void SetEnablePreview(bool value)
        {
            if (EnablePreview == value)
                return;
            EnablePreview = value;
            Save(true);
        }

        private void OnDisable()
        {
            Save(true);
        }
    }
}
