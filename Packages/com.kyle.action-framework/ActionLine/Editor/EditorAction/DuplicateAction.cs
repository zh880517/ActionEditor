using UnityEngine;

namespace ActionLine.EditorView
{
    public class DuplicateAction : EditorAction
    {
        public override string MenuPath => "Duplicate";
        public override int ShowOrder => 3;
        public override bool ShowShortCut => true;
        public override KeyCode ShortCutKey => KeyCode.D;
        public override bool ActionKey => true;
        public override bool Visable(ActionModeType mode)
        {
            return true;
        }

        public override bool IsValid(ActionModeType mode)
        {
            return Context.SelectedClips.Count > 0 || Context.SelectedTracks.Count > 0;
        }

        public override void Execute(ActionModeType mode)
        {
            ActionLineEditorUtil.Dumplicate(Context);
        }
    }
}
