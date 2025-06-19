using UnityEditor;
using UnityEngine;

namespace ActionLine.EditorView
{
    public class PasteAction : EditorAction
    {
        public override string MenuPath => "Paste";
        public override int ShowOrder => 2;
        public override bool ShowShortCut => true;
        public override KeyCode ShortCutKey => KeyCode.V;
        public override bool ActionKey => true;
        public override bool Visable(ActionModeType mode)
        {
            return true;
        }
        public override bool IsValid(ActionModeType mode)
        {
            return ActionLineClipBoard.HasCopyData(MonoScript.FromScriptableObject(Context.Target));
        }
        public override void Execute(ActionModeType mode)
        {
            ActionLineEditorUtil.PasteFromClipboard(Context);
        }
    }
}
