using ActionLine.EditorView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class VisualElementTest : EditorWindow
{
    [MenuItem("Tools/VisualElementTest")]
    private static void Test()
    {
        GetWindow<VisualElementTest>();
    }

    private void CreateGUI()
    {
        //var acticonClip = new ActionClipView();
        //acticonClip.style.left = 50;
        //acticonClip.style.top = 50;
        //acticonClip.style.height = 30;
        //acticonClip.style.width = 300;
        //rootVisualElement.Add(acticonClip);
        var floatField = new FloatField("Ëõ·Å");
        floatField.value = 1.0f;
        rootVisualElement.Add(floatField);
        var timelineBar = new TimelineBarView();
        timelineBar.FrameCount = 52;
        timelineBar.style.flexGrow = 1;
        rootVisualElement.Add(timelineBar);
        floatField.RegisterValueChangedCallback(evt =>
        {
            timelineBar.Scale = evt.newValue;
        });

    }
}
