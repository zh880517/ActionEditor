using UnityEngine;

namespace ActionLine.EditorView
{
    public class SelectAllAction : EditorAction
    {
        public override string MenuPath => "Select All";
        public override int ShowOrder => 5;
        public override KeyCode ShortCutKey => KeyCode.A;
        public override bool ActionKey => true;
        public override bool Visable(ActionModeType mode)
        {
            return true;
        }
        public override bool IsValid(ActionModeType mode)
        {
            return true;
        }
        public override void Execute(ActionModeType mode)
        {
            switch(mode)
            {
                case ActionModeType.Clip:
                case ActionModeType.ClipEmpty:
                    ActionLineSelectUtil.SelectAll(Context, true, false);
                    break;
                case ActionModeType.TrackTitle:
                case ActionModeType.TrackTitleEmpty:
                    ActionLineSelectUtil.SelectAll(Context, false, true);
                    break;
                case ActionModeType.ShortCut:
                    ActionLineSelectUtil.SelectAll(Context, true, true);
                    break;
            }
        }
    }
}
