using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class MotionListView : VisualElement
    {
        private readonly ScrollView scrollView = new ScrollView();
        private readonly Button createButton = new Button();
        private LiteAnimAsset target;
        private readonly List<LiteAnimMotion> motions = new List<LiteAnimMotion>();
        private readonly List<Toggle> toggles = new List<Toggle>();
        public MotionListView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;

            var toolRow = new VisualElement();
            toolRow.style.flexDirection = FlexDirection.Row;
            toolRow.style.paddingLeft = 4;
            toolRow.style.paddingRight = 4;
            toolRow.style.paddingTop = 4;
            toolRow.style.paddingBottom = 4;

            createButton.text = "创建Motion";
            createButton.clicked += () =>
            {
                if (target)
                {
                    LitAnimEditorUtil.CreateMotion(target, "NewMotion");
                    Select(target.Motions.Count - 1);
                }
            };
            toolRow.Add(createButton);
            Add(toolRow);

            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(scrollView);
        }

        public void Refresh(LiteAnimAsset asset, int selectedIndex)
        {
            target = asset;
            motions.Clear();
            toggles.Clear();
            scrollView.Clear();

            if (target != null && target.Motions != null)
            {
                foreach (var m in target.Motions)
                {
                    motions.Add(m);
                }
            }

            for (int i = 0; i < motions.Count; i++)
            {
                int idx = i;
                var motion = motions[i];
                var toggle = new Toggle();
                if (!motion)
                {
                    toggle.text = "<Missing Motion>";
                }
                else
                {
                    string typeText = motion.Type.ToString();
                    toggle.text = $"{motion.name} ({typeText})";
                }
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        Select(idx);
                    }
                    else
                    {
                        toggle.SetValueWithoutNotify(true);
                    }
                });
                toggles.Add(toggle);
                scrollView.Add(toggle);
            }
            createButton.style.display = target ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Select(int index)
        {
            using var evt = MotionSelectEvent.GetPooled(index);
            evt.target = this;
            SendEvent(evt);
        }

    }
}
