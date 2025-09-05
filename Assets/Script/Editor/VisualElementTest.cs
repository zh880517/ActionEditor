using ActionLine;
using ActionLine.EditorView;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class VisualElementTest : EditorWindow
{
    public class PropertyEditorTest
    {
        public int intValue;
        public float floatValue;
        public string stringValue;
        public bool boolValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Color colorValue;
        public AnimationCurve curveValue;
        public Gradient gradientValue;
        public LayerMask layerMaskValue;
        public GameObject gameObjectValue;
        [DisplayName("�����б�")]
        public List<int> intList;
        [DisplayName("�ַ�������"), FixedArraySize]
        public string[] stringArray = new string[5];
        [IntPopupSelect(new string[] { "ѡ��1", "ѡ��2", "ѡ��3" })]
        public int popupValue;
    }

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
        //var floatField = new FloatField("����");
        //floatField.value = 1.0f;
        //rootVisualElement.Add(floatField);
        //var intField = new IntegerField("֡��");
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

        /*
        var actionLineView = new ActionLineView();
        rootVisualElement.Add(actionLineView);
        actionLineView.SetMaxFrameCount(500);
        actionLineView.style.flexGrow = 1;
        */

        PropertyEditorTest test = new PropertyEditorTest();
        var editorElement = PropertyEditor.PropertyElementFactory.CreateByType(typeof(PropertyEditorTest));
        editorElement.SetValue(test);
        rootVisualElement.Add(editorElement);
    }

    private void OnEnable()
    {
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
