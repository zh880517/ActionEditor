using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class MotionListView : VisualElement
    {
        private readonly ScrollView scrollView = new ScrollView();
        private readonly Button createButton = new Button();
        private LiteAnimAsset target;
        private readonly List<ToolbarToggle> toggles = new List<ToolbarToggle>();
        public MotionListView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;

            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(scrollView);

            createButton.text = "创建Motion";
            createButton.clicked += () =>
            {
                if (target)
                {
                    LitAnimEditorUtil.CreateMotion(target, "NewMotion");
                    Select(target.Motions.Count - 1);
                }
            };
            Add(createButton);
        }

        public void Refresh(LiteAnimAsset asset, int selectedIndex)
        {
            target = asset;

            var newMotions = (target != null && target.Motions != null)
                ? (IList<LiteAnimMotion>)target.Motions
                : System.Array.Empty<LiteAnimMotion>();

            // Grow: create new toggles as needed
            for (int i = toggles.Count; i < newMotions.Count; i++)
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

            // Update visible toggles, hide extras
            for (int i = 0; i < toggles.Count; i++)
            {
                if (i < newMotions.Count)
                {
                    var motion = newMotions[i];
                    toggles[i].text = motion == null
                        ? "<Missing Motion>"
                        : $"{motion.name} ({motion.Type})";
                    toggles[i].SetValueWithoutNotify(i == selectedIndex);
                    toggles[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    toggles[i].style.display = DisplayStyle.None;
                }
            }

            createButton.style.display = target ? DisplayStyle.Flex : DisplayStyle.None;

            // Scroll selected item into view
            if (selectedIndex >= 0 && selectedIndex < newMotions.Count)
                scrollView.ScrollTo(toggles[selectedIndex]);
        }

        private void Select(int index)
        {
            using var evt = MotionSelectEvent.GetPooled(index);
            evt.target = this;
            SendEvent(evt);
        }

    }
}
