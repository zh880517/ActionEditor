using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Timeline;

namespace LiteAnim.EditorView
{
    [EditorWindowTitle(title = "LiteAnim Editor")]
    public class LiteAnimAssetEditorWindow : EditorWindow
    {
        [SerializeField]
        private LiteAnimAsset target;
        [SerializeField]
        private int selectedMotionIndex = -1;
        [SerializeField]
        private LiteAnimPreview preview;

        public LiteAnimAsset Target => target;
        public int SelectedMotionIndex => selectedMotionIndex;
        public LiteAnimMotion SelectedMotion
        {
            get
            {
                if (!target || selectedMotionIndex < 0 || selectedMotionIndex >= target.Motions.Count)
                    return null;
                return target.Motions[selectedMotionIndex];
            }
        }

        private ObjectField assetField;
        private ObjectField previewModelField;
        private ToolbarToggle previewToggle;
        private MotionListView motionListView;
        private AssetPropertiesView assetPropertiesView;
        private MotionDetailView motionDetailView;

        private VisualElement motionTabContainer;
        private VisualElement assetTabContainer;
        private ToolbarToggle motionTabToggle;
        private ToolbarToggle assetTabToggle;

        [MenuItem("Tools/LiteAnim/LiteAnim Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<LiteAnimAssetEditorWindow>();
            window.Show();
        }

        [UnityEditor.Callbacks.OnOpenAsset(0)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            if (EditorUtility.EntityIdToObject(instanceID) is LiteAnimAsset asset)
            {
                var window = GetWindow<LiteAnimAssetEditorWindow>();
                window.Show();
                window.Open(asset);
                return true;
            }
            return false;
        }

        private void Awake()
        {
            preview = CreateInstance<LiteAnimPreview>();
            preview.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDestroy()
        {
            if (preview)
                DestroyImmediate(preview);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RefreshUI;
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            CreateToolbar();
            CreateBody();
            RefreshUI();
        }

        private void SetTarget(LiteAnimAsset asset)
        {
            if (target == asset)
            {
                return;
            }
            Undo.RegisterCompleteObjectUndo(this, "open litanim asset");
            target = asset;
            selectedMotionIndex = 0;
            RefreshUI();
            if (Target)
                titleContent = new GUIContent($"LiteAnim Editor - {AssetDatabase.GetAssetPath(Target)}");
            else
                titleContent = new GUIContent("LiteAnim Editor");
        }

        private void CreateToolbar()
        {
            var toolbar = new Toolbar();

            assetField = new ObjectField("Asset")
            {
                objectType = typeof(LiteAnimAsset),
                allowSceneObjects = false
            };
            assetField.style.minWidth = 260;
            assetField.RegisterValueChangedCallback(OnAssetFieldChanged);
            toolbar.Add(assetField);

            var pingButton = new Button(() =>
            {
                if (Target)
                {
                    EditorGUIUtility.PingObject(Target);
                    Selection.activeObject = Target;
                }
            })
            {
                text = "Select"
            };
            toolbar.Add(pingButton);

            previewModelField = new ObjectField("预览模型")
            {
                objectType = typeof(GameObject),
            };
            previewModelField.RegisterValueChangedCallback(evt =>
            {
                var prefab = evt.newValue as GameObject;
                LiteAnimPreviewSetting.instance.AddBind(Target, prefab);
                preview?.OnPreviewChange(prefab, LiteAnimPreviewSetting.instance.EnablePreview);
            });
            toolbar.Add(previewModelField);

            previewToggle = new ToolbarToggle { text = "在场景中预览" };
            previewToggle.RegisterValueChangedCallback(evt =>
            {
                LiteAnimPreviewSetting.instance.SetEnablePreview(evt.newValue);
                var prefab = Target ? LiteAnimPreviewSetting.instance.GetBindTarget(Target) : null;
                preview?.OnPreviewChange(prefab, evt.newValue);
            });
            toolbar.Add(previewToggle);

            rootVisualElement.Add(toolbar);
        }

        private void CreateBody()
        {
            var bodySplit = new TwoPaneSplitView(0, 360, TwoPaneSplitViewOrientation.Horizontal);
            bodySplit.style.flexGrow = 1;
            bodySplit.style.flexShrink = 1;

            var leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;
            leftPane.style.flexShrink = 1;
            leftPane.style.flexDirection = FlexDirection.Column;

            var tabToolbar = new Toolbar();
            motionTabToggle = new ToolbarToggle { text = "Motions" };
            assetTabToggle = new ToolbarToggle { text = "Properties" };
            motionTabToggle.RegisterValueChangedCallback(_ => SetTab(true));
            assetTabToggle.RegisterValueChangedCallback(_ => SetTab(false));
            tabToolbar.Add(motionTabToggle);
            tabToolbar.Add(assetTabToggle);
            leftPane.Add(tabToolbar);

            motionTabContainer = new VisualElement();
            motionTabContainer.style.flexGrow = 1;
            motionTabContainer.style.flexShrink = 1;
            motionListView = new MotionListView();
            motionTabContainer.Add(motionListView);

            assetTabContainer = new VisualElement();
            assetTabContainer.style.flexGrow = 1;
            assetTabContainer.style.flexShrink = 1;
            assetPropertiesView = new AssetPropertiesView();
            assetTabContainer.Add(assetPropertiesView);

            leftPane.Add(motionTabContainer);
            leftPane.Add(assetTabContainer);

            motionDetailView = new MotionDetailView();

            bodySplit.Add(leftPane);
            bodySplit.Add(motionDetailView);
            rootVisualElement.Add(bodySplit);

            SetTab(true);

            rootVisualElement.RegisterCallback<ViewRefeshEvent>(OnViewRefeshEvent);
            rootVisualElement.RegisterCallback<MotionSelectEvent>(OnMotionSelectChange);
            rootVisualElement.RegisterCallback<FrameIndexChangeEvent>(OnFrameIndexChange);
            rootVisualElement.RegisterCallback<AnimParamValueChangedEvent>(OnAnimParamValueChanged);
        }

        private void SetTab(bool showMotion)
        {
            if (motionTabToggle != null)
                motionTabToggle.SetValueWithoutNotify(showMotion);
            if (assetTabToggle != null)
                assetTabToggle.SetValueWithoutNotify(!showMotion);

            if (motionTabContainer != null)
                motionTabContainer.style.display = showMotion ? DisplayStyle.Flex : DisplayStyle.None;
            if (assetTabContainer != null)
                assetTabContainer.style.display = showMotion ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void Open(LiteAnimAsset asset)
        {
            SetTarget(asset);
        }

        private void OnAssetFieldChanged(ChangeEvent<Object> evt)
        {
            Open(evt.newValue as LiteAnimAsset);
        }
        private void OnViewRefeshEvent(ViewRefeshEvent evt)
        {
            RefreshUI();
        }

        private void OnMotionSelectChange(MotionSelectEvent evt)
        {
            if(selectedMotionIndex == evt.SelectedIndex)
                return;
            selectedMotionIndex = evt.SelectedIndex;
            RefreshUI();
            preview.Evaluate(SelectedMotion, 0);
        }

        private void OnAnimParamValueChanged(AnimParamValueChangedEvent evt)
        {
            if (preview && SelectedMotion != null && !string.IsNullOrEmpty(SelectedMotion.Param))
            {
                preview.SetParam(SelectedMotion.Param, evt.Value);
            }
        }

        private const int FrameRate = 30;

        private void OnFrameIndexChange(FrameIndexChangeEvent evt)
        {
            if (!preview || SelectedMotion == null)
                return;
            float time = evt.Frame / (float)FrameRate;
            preview.Evaluate(SelectedMotion, time);
        }

        private void RefreshUI()
        {
            assetField?.SetValueWithoutNotify(Target);
            var prefab = Target ? LiteAnimPreviewSetting.instance.GetBindTarget(Target) : null;
            if (previewModelField != null)
            {

                previewModelField.SetValueWithoutNotify(prefab);
                previewModelField.SetEnabled(target != null);
            }
            if(previewToggle != null)
            {
                previewToggle.SetValueWithoutNotify(LiteAnimPreviewSetting.instance.EnablePreview);
                previewToggle.style.display = prefab ? DisplayStyle.Flex : DisplayStyle.None;
            }
            preview.OnPreviewChange(prefab, LiteAnimPreviewSetting.instance.EnablePreview);
            assetPropertiesView?.Bind(Target);
            motionListView?.Refresh(Target, SelectedMotionIndex);
            motionDetailView?.RefrshView(Target, SelectedMotion);
        }
    }
}
