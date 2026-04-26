using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    // 配置编辑器主窗口
    // 菜单入口：Tools/GOAP Editor
    // 布局：
    //   顶部工具栏（选择 Config、新增、导出）
    //   中部左右分栏（Goals 卡片组 | Actions 卡片组）
    public class GOAPWindow : EditorWindow
    {
        private ConfigAsset _currentConfig;
        private ScrollView _goalsScrollView;
        private ScrollView _actionsScrollView;

        [MenuItem("Tools/GOAP Editor")]
        public static void Open()
        {
            var window = GetWindow<GOAPWindow>("GOAP Editor");
            window.minSize = new Vector2(700, 500);
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = 8;

            // 工具栏
            root.Add(BuildToolbar());

            // 左右分栏
            var columns = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            columns.style.flexGrow = 1;
            columns.style.marginTop = 8;

            // Goals 列
            var goalsPanel = BuildGroupPanel("Goals", out _goalsScrollView);
            // Actions 列
            var actionsPanel = BuildGroupPanel("Actions", out _actionsScrollView);

            columns.Add(goalsPanel);
            columns.Add(actionsPanel);
            root.Add(columns);

            RefreshCards();
        }

        // 工具栏：Config 选择器 + 新增按钮 + 保存/导出按钮
        private VisualElement BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.marginBottom = 4;

            var configLabel = new Label("Config:");
            configLabel.style.marginRight = 4;

            var configField = new UnityEditor.UIElements.ObjectField
            {
                objectType = typeof(ConfigAsset),
                value = _currentConfig
            };
            configField.style.flexGrow = 1;
            configField.RegisterValueChangedCallback(e =>
            {
                _currentConfig = e.newValue as ConfigAsset;
                RefreshCards();
            });

            var addActionBtn = new Button(() => ShowAddActionMenu()) { text = "+ Action" };
            var addGoalBtn = new Button(() => ShowAddGoalMenu()) { text = "+ Goal" };

            var saveBtn = new Button(() => SaveAndExport()) { text = "保存/导出" };
            saveBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f));

            toolbar.Add(configLabel);
            toolbar.Add(configField);
            toolbar.Add(addGoalBtn);
            toolbar.Add(addActionBtn);
            toolbar.Add(saveBtn);
            return toolbar;
        }

        // 创建分组面板（含标题 + 滚动视图）
        private VisualElement BuildGroupPanel(string title, out ScrollView scrollView)
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.paddingLeft = panel.style.paddingRight = 4;

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14;
            titleLabel.style.marginBottom = 4;

            scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;

            panel.Add(titleLabel);
            panel.Add(scrollView);
            return panel;
        }

        // 刷新所有卡片（切换 Config 或数据变更后调用）
        private void RefreshCards()
        {
            _goalsScrollView?.Clear();
            _actionsScrollView?.Clear();

            if (_currentConfig == null)
                return;

            foreach (var goal in _currentConfig.Goals)
            {
                var card = new GoalCardView(goal, _currentConfig.BoolKeyType, _currentConfig.IntKeyType);
                var capturedGoal = goal;
                card.RegisterCallback<DataChangedEvent>(_ => MarkDirty());
                card.RegisterCallback<DeleteRequestEvent>(_ =>
                {
                    _currentConfig.Goals.Remove(capturedGoal);
                    AssetDatabase.RemoveObjectFromAsset(capturedGoal);
                    DestroyImmediate(capturedGoal);
                    MarkDirty();
                    AssetDatabase.SaveAssets();
                    RefreshCards();
                });
                _goalsScrollView.Add(card);
            }

            foreach (var action in _currentConfig.Actions)
            {
                var card = new ActionCardView(action, _currentConfig.BoolKeyType, _currentConfig.IntKeyType);
                var capturedAction = action;
                card.RegisterCallback<DataChangedEvent>(_ => MarkDirty());
                card.RegisterCallback<DeleteRequestEvent>(_ =>
                {
                    _currentConfig.Actions.Remove(capturedAction);
                    AssetDatabase.RemoveObjectFromAsset(capturedAction);
                    DestroyImmediate(capturedAction);
                    MarkDirty();
                    AssetDatabase.SaveAssets();
                    RefreshCards();
                });
                _actionsScrollView.Add(card);
            }
        }

        // 弹出 Action 类型选择菜单（按 GOAPTag 过滤）
        private void ShowAddActionMenu()
        {
            if (_currentConfig == null) return;

            var tagAttr = _currentConfig.GetType().GetCustomAttribute<GOAPTagAttribute>();
            var menu = new GenericMenu();

            if (tagAttr == null)
            {
                menu.AddDisabledItem(new GUIContent("当前 Config 未标注 [GOAPTag]"));
                menu.ShowAsContext();
                return;
            }

            // 收集允许的分组名
            var allowedGroups = new HashSet<string>();
            foreach (var groupType in tagAttr.GroupTypes)
            {
                var ga = Activator.CreateInstance(groupType) as ActionGroupAttribute;
                if (ga != null) allowedGroups.Add(ga.Name);
            }

            // 枚举 ActionData 非抽象子类，按分组过滤并排序
            var grouped = new SortedDictionary<(int order, string name), List<(string groupName, Type type)>>(
                Comparer<(int, string)>.Create((a, b) =>
                {
                    int c = a.Item1.CompareTo(b.Item1);
                    return c != 0 ? c : string.Compare(a.Item2, b.Item2, StringComparison.Ordinal);
                }));

            foreach (var type in TypeCache.GetTypesDerivedFrom<ActionData>())
            {
                if (type.IsAbstract) continue;
                var ga = type.GetCustomAttribute<ActionGroupAttribute>();
                if (ga == null || !allowedGroups.Contains(ga.Name)) continue;
                var key = (ga.Order, ga.Name);
                if (!grouped.TryGetValue(key, out var list))
                    grouped[key] = list = new List<(string, Type)>();
                list.Add((ga.Name, type));
            }

            foreach (var kvp in grouped)
                foreach (var (groupName, type) in kvp.Value)
                {
                    var t = type;
                    menu.AddItem(new GUIContent($"{groupName}/{ObjectNames.NicifyVariableName(type.Name)}"), false,
                        () => AddActionOfType(t));
                }

            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("没有可用的 Action 类型"));

            menu.ShowAsContext();
        }

        // 创建指定类型的 ActionData 子资产并加入 Config
        private void AddActionOfType(Type type)
        {
            if (_currentConfig == null) return;
            var action = CreateInstance(type) as ActionData;
            action.name = type.Name;
            action.hideFlags = HideFlags.HideInHierarchy;
            var path = AssetDatabase.GetAssetPath(_currentConfig);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.AddObjectToAsset(action, _currentConfig);
            _currentConfig.Actions.Add(action);
            MarkDirty();
            AssetDatabase.SaveAssets();
            RefreshCards();
        }

        // 弹出 Goal 类型选择菜单
        private void ShowAddGoalMenu()
        {
            if (_currentConfig == null) return;
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<GoalData>())
            {
                if (type.IsAbstract) continue;
                var t = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false,
                    () => AddGoalOfType(t));
            }
            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("没有可用的 Goal 类型"));
            menu.ShowAsContext();
        }

        // 创建指定类型的 GoalData 子资产并加入 Config
        private void AddGoalOfType(Type type)
        {
            if (_currentConfig == null) return;
            var goal = CreateInstance(type) as GoalData;
            goal.name = type.Name;
            goal.hideFlags = HideFlags.HideInHierarchy;
            var path = AssetDatabase.GetAssetPath(_currentConfig);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.AddObjectToAsset(goal, _currentConfig);
            _currentConfig.Goals.Add(goal);
            MarkDirty();
            AssetDatabase.SaveAssets();
            RefreshCards();
        }

        private void MarkDirty()
        {
            if (_currentConfig != null)
                EditorUtility.SetDirty(_currentConfig);
        }

        private void SaveAndExport()
        {
            if (_currentConfig == null)
            {
                Debug.LogWarning("[GOAP] 请先选择一个 ConfigAsset");
                return;
            }
            AssetDatabase.SaveAssets();
            Exporter.Export(_currentConfig);
        }
    }
}
