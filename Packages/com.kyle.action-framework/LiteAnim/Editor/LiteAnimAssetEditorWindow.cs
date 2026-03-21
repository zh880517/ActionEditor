using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    [EditorWindowTitle(title = "LiteAnim Editor")]
    public class LiteAnimAssetEditorWindow : EditorWindow
    {
        [SerializeField]
        private LiteAnimAsset target;
        [SerializeField]
        private int selectedMotionIndex = -1;

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

        private event System.Action OnChanged;

        private ObjectField assetField;
        private ObjectField motionField;

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

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            OnChanged += RefreshUI;
            Refresh();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            OnChanged -= RefreshUI;
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            CreateToolbar();
            CreateBody();
            RefreshUI();
        }

        private void Refresh()
        {
            ClampSelection();
            OnChanged?.Invoke();
        }

        private void SetTarget(LiteAnimAsset asset)
        {
            if (target == asset)
            {
                ClampSelection();
                OnChanged?.Invoke();
                return;
            }

            target = asset;
            selectedMotionIndex = -1;
            ClampSelection();
            OnChanged?.Invoke();
        }


        private void ClampSelection()
        {
            int count = GetMotionCount();
            if (count == 0)
            {
                selectedMotionIndex = -1;
                return;
            }
            selectedMotionIndex = Mathf.Clamp(selectedMotionIndex, 0, count - 1);
        }

        private int GetMotionCount()
        {
            if (!target)
                return 0;
            return target.Motions.Count;
        }

        private void OnUndoRedoPerformed()
        {
            Refresh();
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

            motionField = new ObjectField("Motion")
            {
                objectType = typeof(LiteAnimMotion),
                allowSceneObjects = false
            };
            motionField.style.minWidth = 260;
            toolbar.Add(motionField);


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
            Refresh();
        }

        private void OnMotionSelectChange(MotionSelectEvent evt)
        {
            if(selectedMotionIndex == evt.SelectedIndex)
                return;
            selectedMotionIndex = evt.SelectedIndex;
            Refresh();
        }

        private void RefreshUI()
        {
            assetField?.SetValueWithoutNotify(Target);
            assetPropertiesView?.Bind(Target);
            motionListView?.Refresh(Target, SelectedMotionIndex);
            motionDetailView?.RefrshView(SelectedMotion);

            if (Target)
            {
                titleContent = new GUIContent($"LiteAnim Editor - {AssetDatabase.GetAssetPath(Target)}");
            }
            else
            {
                titleContent = new GUIContent("LiteAnim Editor");
            }
        }

    }
}
