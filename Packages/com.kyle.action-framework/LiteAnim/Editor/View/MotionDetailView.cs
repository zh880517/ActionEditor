using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class MotionDetailView : VisualElement
    {
        private LiteAnimMotion target;
        private LiteAnimAsset asset;
        private readonly TextField nameField = new TextField("Name");
        private readonly PopupField<int> layerIndexField;
        private readonly Toggle loopToggle = new Toggle("Loop");
        private readonly EnumField typeField = new EnumField("Type", MotionType.Clip);
        private readonly TextField paramField = new TextField("Param");
        private MotionEditorView motionEditor;

        public MotionDetailView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;

            layerIndexField = new PopupField<int>("Layer Index", new List<int> { 0 }, 0,
                FormatLayerIndex, FormatLayerIndex);

            Add(nameField);
            Add(layerIndexField);
            Add(loopToggle);
            Add(typeField);
            Add(paramField);

            nameField.RegisterValueChangedCallback(OnNameChanged);
            nameField.isDelayed = true;
            layerIndexField.RegisterValueChangedCallback(OnLayerIndexChanged);
            loopToggle.RegisterValueChangedCallback(OnLoopChanged);
            typeField.RegisterValueChangedCallback(OnTypeChanged);
            paramField.RegisterValueChangedCallback(OnParamChanged);
            paramField.isDelayed = true;
        }

        public void RefrshView(LiteAnimAsset asset, LiteAnimMotion motion)
        {
            target = motion;
            this.asset = asset;
            bool hasTarget = motion != null;
            nameField.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            layerIndexField.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            loopToggle.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            typeField.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            paramField.style.display = DisplayStyle.None;
            if(motionEditor != null)
            {
                motionEditor.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!hasTarget)
                return;

            nameField.SetValueWithoutNotify(motion.name);
            RefreshLayerIndexField(motion.LayerIndex);
            loopToggle.SetValueWithoutNotify(motion.Loop);
            typeField.SetValueWithoutNotify(motion.Type);
            paramField.SetValueWithoutNotify(motion.Param ?? string.Empty);

            paramField.style.display = motion.Type == MotionType.BlendTree ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshMotionEditorView();
        }

        private void RefreshMotionEditorView()
        {
            if(motionEditor == null || motionEditor.Type != target.Type)
            {
                motionEditor?.RemoveFromHierarchy();
                switch(target.Type)
                {
                    case MotionType.Clip:
                        motionEditor = new ClipMotionEditorView();
                            break;
                    case MotionType.BlendTree:
                        motionEditor = new BlendTreeMotionEditorView();
                        break;
                }
                Add(motionEditor);
            }
            motionEditor.Refresh(target);
        }

        private string FormatLayerIndex(int i)
        {
            if (asset != null && i >= 0 && i < asset.Layers.Count)
                return $"{i}:{asset.Layers[i].LayerName}";
            return i.ToString();
        }

        private void RefreshLayerIndexField(int currentIndex)
        {
            var choices = new List<int>();
            int layerCount = asset != null ? asset.Layers.Count : 1;
            for (int i = 0; i < layerCount; i++)
                choices.Add(i);
            if (!choices.Contains(currentIndex))
                choices.Add(currentIndex);
            layerIndexField.choices = choices;

            bool isInvalid = currentIndex != 0 && (asset == null || currentIndex >= asset.Layers.Count);
            layerIndexField.style.backgroundColor = isInvalid ? new Color(0.6f, 0.1f, 0.1f, 1f) : StyleKeyword.Null;

            layerIndexField.SetValueWithoutNotify(currentIndex);
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            if (target == null) return;
            if (evt.newValue == target.name) return;
            if (evt.newValue.Length == 0)
            {
                EditorUtility.DisplayDialog("Invalid Name", "Motion name cannot be empty.", "OK");
                nameField.SetValueWithoutNotify(target.name);
                return;
            }

            LitAnimEditorUtil.RegisterUndo(target, "Rename Motion");
            target.name = evt.newValue;
            target.OnModify();
            AssetDatabase.SaveAssets();
            ViewRefeshEvent.Dispatch(this);
        }

        private void OnLayerIndexChanged(ChangeEvent<int> evt)
        {
            if (target == null) return;
            LitAnimEditorUtil.RegisterUndo(target, "Edit Motion Layer Index");
            target.LayerIndex = evt.newValue;
            target.OnModify();
            bool isInvalid = evt.newValue != 0 && (asset == null || evt.newValue >= asset.Layers.Count);
            layerIndexField.style.backgroundColor = isInvalid ? new Color(0.6f, 0.1f, 0.1f, 1f) : StyleKeyword.Null;
        }

        private void OnLoopChanged(ChangeEvent<bool> evt)
        {
            if (target == null) return;
            LitAnimEditorUtil.RegisterUndo(target, "Edit Motion Loop");
            target.Loop = evt.newValue;
            target.OnModify();
        }

        private void OnTypeChanged(ChangeEvent<System.Enum> evt)
        {
            if (target == null) return;
            LitAnimEditorUtil.RegisterUndo(target, "Edit Motion Type");
            target.Type = (MotionType)evt.newValue;
            target.OnModify();
            paramField.style.display = target.Type == MotionType.BlendTree ? DisplayStyle.Flex : DisplayStyle.None;
            ViewRefeshEvent.Dispatch(this);
        }

        private void OnParamChanged(ChangeEvent<string> evt)
        {
            if (target == null) return;
            LitAnimEditorUtil.RegisterUndo(target, "Edit Motion Param");
            target.Param = evt.newValue;
            target.OnModify();
        }
    }
}
