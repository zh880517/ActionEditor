using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class ClipPropertyView : VisualElement
    {
        private readonly Foldout foldout = new Foldout();
        private readonly Image icon = new Image();
        private InspectorElement inspector;
        private ActionLineClip clip;

        public ClipPropertyView()
        {
            style.borderTopWidth = 1;
            style.borderTopColor = Color.black;
            foldout.RegisterValueChangedCallback(OnValueChanged);
            var toggle = foldout.Q<Toggle>();
            toggle.style.borderTopColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            toggle.style.borderTopWidth = 0.5f;
            Add(foldout);
            var lable = toggle.Q<Label>();
            int index = lable.parent.IndexOf(lable);
            icon.style.marginLeft = 5;
            icon.style.marginRight = 5;
            lable.parent.Insert(index, icon);
        }

        public void SetClip(ActionLineClip clip, bool editorable)
        {
            if (this.clip == clip)
                return;
            if(this.clip)
            {
                inspector.RemoveFromHierarchy();
                inspector = null;
                this.clip = null;
            }
            this.clip = clip;
            if(!clip)
            {
                style.display = DisplayStyle.None;
            }
            else
            {
                foldout.SetValueWithoutNotify(clip.Foldout);
                var typeInfo = ActionClipTypeUtil.GetTypeInfo(clip.GetType());
                foldout.text = clip.name;
                icon.image = typeInfo.Icon;
                style.display = DisplayStyle.Flex;
                inspector = new InspectorElement(clip);
                inspector.SetEnabled(editorable);
                //TODO:修改背景色或者添加文本提示框提示不可修改
                foldout.Add(inspector);
            }
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            if(clip)
            {
                clip.Foldout = evt.newValue;
            }
        }
    }
}
