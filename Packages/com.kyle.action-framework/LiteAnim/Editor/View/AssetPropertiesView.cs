using PropertyEditor;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class AssetPropertiesView : VisualElement
    {
        private readonly ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
        private StructedPropertyElement propertyEditor;
        private LiteAnimAsset currentAsset;

        public AssetPropertiesView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;
            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;
            Add(scrollView);
        }

        public void Bind(LiteAnimAsset asset)
        {
            if (currentAsset == asset)
                return;

            scrollView.Clear();
            currentAsset = asset;

            if (asset == null)
            {
                if (propertyEditor != null)
                {
                    propertyEditor.style.display = DisplayStyle.None;
                }
                return;
            }
            if (propertyEditor == null)
            {
                propertyEditor = PropertyElementFactory.CreateByUnityObject(asset, false) as StructedPropertyElement;
                propertyEditor.RegisterCallback<RegisterUndoEvent>(OnRegisterUndoEvent);
                scrollView.Add(propertyEditor);
            }
            propertyEditor.SetValue(asset);
        }

        private void OnRegisterUndoEvent(RegisterUndoEvent evt)
        {
            if (currentAsset != null)
            {
                LitAnimEditorUtil.RegisterUndo(currentAsset, evt.ActionName);
                ViewRefeshEvent.Dispatch(this);
            }
        }
    }
}
