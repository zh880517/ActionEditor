using UnityEngine;

namespace ActionLine.EditorView
{
    public enum ActionModeType
    {
        Clip,
        ClipEmpty,
        TrackTitle,
        TrackTitleEmpty,
        ShortCut,
    }

    public abstract class EditorAction
    {
        public ActionLineEditorContext Context;

        //右键菜单显示
        public virtual int ShowOrder => 0; //菜单显示顺序，数字越小越靠前 每100个分一组
        public abstract string MenuPath { get; }//如果为null，则不显示在右键菜单上
        public virtual Texture Icon => null;//图标，如果为null，则不显示图标

        public virtual bool ShowShortCut => true; //是否显示快捷键
        public virtual KeyCode ShortCutKey => KeyCode.None; //快捷键
        public virtual bool ShiftKey => false; //是否需要 按着 Shift 才能触发执行
        public virtual bool ActionKey => false;//是否需要 ActionKey 才能触发执行（ctrl 或者 Command）

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
