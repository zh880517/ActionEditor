using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ActionLine.EditorView
{
    public class PropertyView : VisualElement
    {
        private readonly ToggleButtonGroup buttonGroup = new ToggleButtonGroup();
        private readonly ScrollView groupScrollView = new ScrollView(ScrollViewMode.Vertical);
        private readonly ScrollView clipScrollView = new ScrollView(ScrollViewMode.Vertical);
        private readonly List<ClipPropertyView> propertyViews = new List<ClipPropertyView>();
        private int visableCount = 0;
        private ActionLineAsset target;
        private InspectorElement targetInspector;
        public PropertyView()
        {
            Add(buttonGroup);
            buttonGroup.Add(new Button() { text = "Clip" });
            buttonGroup.Add(new Button() { text = "Group" });
            buttonGroup.isMultipleSelection = false;
            buttonGroup.RegisterValueChangedCallback(OnToggleStateChange);
            Add(groupScrollView);
            Add(clipScrollView);
            clipScrollView.style.display = DisplayStyle.None;
            style.flexGrow = 1;
            style.flexShrink = 1;
        }

        public void EnsureCapacity(int count)
        {
            while (propertyViews.Count < count)
            {
                var e = new ClipPropertyView();
                e.style.display = DisplayStyle.None;
                clipScrollView.Add(e);
                propertyViews.Add(e);
            }
        }

        public void SetVisableCount(int count)
        {
            if (visableCount == count)
                return;
            EnsureCapacity(count);
            for (int i = count; i < propertyViews.Count; i++)
            {
                var e = propertyViews[i];
                e.SetClip(null, false);
            }
        }

        public void SetClip(int index, ActionLineClip clip, bool editorable)
        {
            var e = propertyViews[index];
            e.SetClip(clip, editorable);
        }

        public void SetAsset(ActionLineAsset asset)
        {
            if (target == asset)
                return;
            if(target)
            {
                targetInspector.RemoveFromHierarchy();
                targetInspector = null;
                target = null;
            }
            target = asset;
            if(asset)
            {
                targetInspector = new InspectorElement(asset);
                groupScrollView.Add(targetInspector);
            }
        }

        private void OnToggleStateChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            if (evt.newValue[0])
            {
                clipScrollView.style.display = DisplayStyle.Flex;
                groupScrollView.style.display = DisplayStyle.None;
            }
            else
            {
                clipScrollView.style.display = DisplayStyle.None;
                groupScrollView.style.display = DisplayStyle.Flex;
            }
        }

    }
}
