using UnityEngine;

namespace ActionLine.EditorView
{
    public enum ActionModeType
    {
        Clip,
        ClipEmpty,
        TrackTitle,
        TrackTitleEmpty,
    }

    public abstract class EditorAction
    {
        public ActionLineEditorContext Context;

        //右键菜单显示
        public abstract string MenuPath { get; }
        public virtual Texture Icon => null;
        public virtual bool Visable(ActionModeType mode) { return false; }
        //Visable 返回true才会判断 IsValid
        public virtual bool IsValid(ActionModeType mode) { return false; }
        //菜单上是否显示对勾
        public virtual bool IsOn(ActionModeType mode) { return false; }

        public virtual void Execute(ActionModeType mode)
        {
        }
    }
}
