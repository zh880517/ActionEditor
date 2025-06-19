using UnityEngine;

namespace ActionLine.EditorView
{
    public class DeleteAction : EditorAction
    {
        public override string MenuPath => "Delete";
        public override int ShowOrder => 4;

        public override KeyCode ShortCutKey => KeyCode.Delete;

        public override bool Visable(ActionModeType mode)
        {
            return true;
        }

        public override bool IsValid(ActionModeType mode)
        {
            switch(mode)
            {
                case ActionModeType.Clip:
                case ActionModeType.ClipEmpty:
                    return Context.SelectedClips.Count > 0;
                case ActionModeType.TrackTitle:
                case ActionModeType.TrackTitleEmpty:
                    return Context.SelectedTracks.Count > 0;
                case ActionModeType.ShortCut:
                    return Context.SelectedClips.Count > 0 || Context.SelectedTracks.Count > 0;
            }
            return false;
        }

        public override void Execute(ActionModeType mode)
        {
            switch (mode)
            {
                case ActionModeType.Clip:
                case ActionModeType.ClipEmpty:
                    ActionLineEditorUtil.DeleteSelectedClips(Context, true, false);
                    break;
                case ActionModeType.TrackTitle:
                case ActionModeType.TrackTitleEmpty:
                    ActionLineEditorUtil.DeleteSelectedClips(Context, false, true);
                    break;
                case ActionModeType.ShortCut:
                    ActionLineEditorUtil.DeleteSelectedClips(Context, true, true);
                    break;
            }
        }
    }
}
