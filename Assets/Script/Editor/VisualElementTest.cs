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

        Debug.LogError("CreateGUI called");
    }

    private void OnEnable()
    {
        Debug.LogError("OnEnable called");
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
