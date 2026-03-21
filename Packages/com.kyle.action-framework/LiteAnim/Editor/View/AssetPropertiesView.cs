using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class AssetPropertiesView : VisualElement
    {
        private readonly ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
        private SerializedObject serializedObject;

        public AssetPropertiesView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;
            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;
            Add(scrollView);
        }

        public void Bind(ScriptableObject asset)
        {
            if (serializedObject.targetObject == asset)
                return;
            scrollView.Unbind();
            scrollView.Clear();
            serializedObject = null;

            if (!asset)
                return;

            serializedObject = new SerializedObject(asset);
            var fadeProperty = serializedObject.FindProperty("DefaultFadeDuration");
            var layersProperty = serializedObject.FindProperty("Layers");

            var fadeField = new PropertyField(fadeProperty);
            fadeField.name = "default-fade-duration";
            var layersField = new PropertyField(layersProperty);
            layersField.name = "layers";

            scrollView.Add(fadeField);
            scrollView.Add(layersField);
            scrollView.Bind(serializedObject);
        }
    }
}
