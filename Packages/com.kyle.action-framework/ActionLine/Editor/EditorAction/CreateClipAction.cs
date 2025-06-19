using UnityEngine;

namespace ActionLine.EditorView
{
    public class CreateClipAction : EditorAction
    {
        public override string MenuPath => "Create";
        public override int ShowOrder => 100000;
        public override bool ShowShortCut => true;
        public override KeyCode ShortCutKey => KeyCode.N;
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
            Context.ShowTypeSelectWindow();
        }
    }
}
