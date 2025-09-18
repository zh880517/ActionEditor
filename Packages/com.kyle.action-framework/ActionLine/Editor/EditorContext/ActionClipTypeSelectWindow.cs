using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ActionLine.EditorView
{
    public class ActionClipTypeSelectWindow : TypeSelectWindow
    {
        public ActionLineEditorContext Context;

        private List<Type> types;

        protected override IEnumerable<Type> GetTypes()
        {
            if(types == null)
            {
                types = new List<Type>();
                var assetType = Context.GetType();
                var typeList = TypeCollector<ActionLineClip>.Types;
                foreach (var type in typeList)
                {
                    var attributes = type.GetCustomAttributes<ActionLineTypeAttribute>(true);
                    foreach (var item in attributes)
                    {
                        if(item.AssetType.IsSubclassOf(assetType))
                        {
                            types.Add(type);
                        }
                    }
                }
            }
            return types;
        }

        protected override void OnSelect(Type type, Vector2 screenMousePosition)
        {
            Context.RegisterUndo("Create ActionLine Clip");
            var clip = ActionLineEditorUtil.CreateClip(type, "Create ActionLine Clip");
            Context.Target.AddClip(clip);
            Context.SelectedClips.Clear();
            Context.SelectedTracks.Clear();
            Context.RefreshView();
            var data = Context.Clips.First(it => it.Clip == clip);
            Context.SelectedTracks.Add(data);
            Context.SelectedClips.Add(data);
            Context.RefreshSelectState();
        }
    }
}
