using ActionLine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class VisualElementTest : EditorWindow
{
    [MenuItem("Tools/VisualElementTest")]
    private static void Test()
    {
        GetWindow<VisualElementTest>();
    }
    private ScriptableObject target;
    private void CreateGUI()
    {
        //var acticonClip = new ActionClipView();
        //acticonClip.style.left = 50;
        //acticonClip.style.height = 30;
        //acticonClip.style.width = 300;
        //rootVisualElement.Add(acticonClip);
        //var floatField = new FloatField("Ëõ·Å");
        //floatField.value = 1.0f;
        //rootVisualElement.Add(floatField);
        //var intField = new IntegerField("Ö¡Êý");
        //rootVisualElement.Add(intField);
        //var timelineBar = new TimelineTickMarkView();
        //timelineBar.style.left = 5;
        //timelineBar.FrameCount = 52;
        //timelineBar.style.flexGrow = 1;
        //rootVisualElement.Add(timelineBar);
        //var cursorView = new TimelineCursorView();
        //timelineBar.SetCursorView(cursorView);
        //timelineBar.Add(cursorView);
        //cursorView.StretchToParentSize();
        //cursorView.ShowFrameRange(12, 10);
        //cursorView.CurrentFrame = 20;
        //floatField.RegisterValueChangedCallback(evt =>
        //{
        //    timelineBar.Scale = evt.newValue;
        //});
        //var trackScrollView = new TrackScrollView();
        //trackScrollView.style.flexGrow = 1;
        //rootVisualElement.Add(trackScrollView);
        //trackScrollView.SetFrameCount(500);
        //trackScrollView.OnScaleChanged = scale =>
        //{
        //    floatField.value = scale;
        //};

        //intField.RegisterValueChangedCallback(evt =>
        //{
        //    trackScrollView.FitFrameInView(evt.newValue);
        //});

        //var actionLineView = new ActionLineView();
        //rootVisualElement.Add(actionLineView);
        //actionLineView.Track.SetFrameCount(500);
        //actionLineView.style.flexGrow = 1;
        Foldout foldout = new Foldout { text = "ActionLineClip Inspector" };
        target = CreateInstance<ActionLine.ActionLineClip>();
        var element = new InspectorElement(target);
        foldout.Add(element);
        var toggle = foldout.Q<Toggle>();
        foldout.style.borderTopWidth = 1;
        foldout.style.borderTopColor = Color.black;
        foldout.style.borderBottomWidth = 1;
        foldout.style.borderBottomColor = Color.black;
        toggle.style.borderBottomWidth = 0.5f;
        toggle.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        var lable = toggle.Q<Label>();
        var icon = new Image();
        icon.image = MonoScriptUtil.GetMonoScriptIcon<ActionLineClip>();
        int index = lable.parent.IndexOf(lable);
        icon.style.marginLeft = 5;
        icon.style.marginRight = 5;
        lable.parent.Insert(index, icon);
        rootVisualElement.Add(foldout);
    }

    private void OnDestroy()
    {
        if (target != null)
        {
            DestroyImmediate(target);
            target = null;
        }
    }
}
