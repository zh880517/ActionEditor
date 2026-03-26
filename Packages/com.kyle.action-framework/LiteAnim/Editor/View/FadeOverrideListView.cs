using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class FadeOverrideListView : VisualElement
    {
        private readonly ScrollView scrollView = new ScrollView();
        private readonly Button addButton = new Button();
        private readonly Button removeButton = new Button();
        private LiteAnimAsset target;
        private int selectedIndex = -1;
        private readonly List<ToolbarToggle> toggles = new List<ToolbarToggle>();

        public FadeOverrideListView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;

            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(scrollView);

            var buttonBar = new VisualElement();
            buttonBar.style.flexDirection = FlexDirection.Row;
            buttonBar.style.justifyContent = Justify.FlexEnd;

            addButton.text = "+";
            addButton.style.width = 24;
            addButton.clicked += OnAdd;
            buttonBar.Add(addButton);

            removeButton.text = "-";
            removeButton.style.width = 24;
            removeButton.clicked += OnRemove;
            buttonBar.Add(removeButton);

            Add(buttonBar);
        }

        public void Refresh(LiteAnimAsset asset, int selected)
        {
            target = asset;
            selectedIndex = selected;

            var overrides = (target != null && target.FadeOverrides != null)
                ? (IList<MotionFadeOverride>)target.FadeOverrides
                : System.Array.Empty<MotionFadeOverride>();

            // 按需创建 toggle
            for (int i = toggles.Count; i < overrides.Count; i++)
            {
                int idx = i;
                var tt = new ToolbarToggle();
                tt.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                        Select(idx);
                    else
                        tt.SetValueWithoutNotify(true);
                });
                toggles.Add(tt);
                scrollView.Add(tt);
            }

            // 更新可见 toggle，隐藏多余的
            for (int i = 0; i < toggles.Count; i++)
            {
                if (i < overrides.Count)
                {
                    var item = overrides[i];
                    string fromName = item.From != null ? item.From.name : "(None)";
                    string toName = item.To != null ? item.To.name : "(None)";
                    toggles[i].text = $"{fromName} → {toName} ({item.FadeDuration:F2}s)";
                    toggles[i].SetValueWithoutNotify(i == selectedIndex);
                    toggles[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    toggles[i].style.display = DisplayStyle.None;
                }
            }

            addButton.style.display = target ? DisplayStyle.Flex : DisplayStyle.None;
            removeButton.style.display = target ? DisplayStyle.Flex : DisplayStyle.None;

            if (selectedIndex >= 0 && selectedIndex < overrides.Count)
                scrollView.ScrollTo(toggles[selectedIndex]);
        }

        private void Select(int index)
        {
            using var evt = FadeOverrideSelectEvent.GetPooled(index);
            evt.target = this;
            SendEvent(evt);
        }

        private void OnAdd()
        {
            if (target == null) return;
            LitAnimEditorUtil.RegisterUndo(target, "Add Fade Override");
            target.FadeOverrides.Add(new MotionFadeOverride
            {
                From = null,
                To = null,
                FadeDuration = target.DefaultFadeDuration
            });
            Select(target.FadeOverrides.Count - 1);
        }

        private void OnRemove()
        {
            if (target == null || selectedIndex < 0 || selectedIndex >= target.FadeOverrides.Count)
                return;
            LitAnimEditorUtil.RegisterUndo(target, "Remove Fade Override");
            target.FadeOverrides.RemoveAt(selectedIndex);
            int newIndex = selectedIndex >= target.FadeOverrides.Count
                ? target.FadeOverrides.Count - 1
                : selectedIndex;
            Select(newIndex);
        }
    }
}
