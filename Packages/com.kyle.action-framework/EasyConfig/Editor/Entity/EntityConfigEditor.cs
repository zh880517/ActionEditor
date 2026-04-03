using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PropertyEditor;

namespace EasyConfig
{
    [CustomEditor(typeof(EntityConfig), true)]
    public class EntityConfigEditor : UnityEditor.Editor
    {
        private VisualElement componentContainer;
        private EntityConfig entityConfig;
        private SerializedProperty componentsProperty;

        private static List<Type> cachedComponentTypes;

        private static List<Type> CollectComponentTypes()
        {
            if (cachedComponentTypes != null)
                return cachedComponentTypes;

            cachedComponentTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }

                if (types == null) continue;
                foreach (var type in types)
                {
                    if (type != null && !type.IsAbstract && !type.IsInterface
                        && type.IsSubclassOf(typeof(ConfigComponent)))
                    {
                        cachedComponentTypes.Add(type);
                    }
                }
            }
            cachedComponentTypes.Sort((a, b) =>
                string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return cachedComponentTypes;
        }

        private static Dictionary<string, List<Type>> CollectGroupedComponentTypes()
        {
            var result = new Dictionary<string, List<Type>>();

            foreach (var type in CollectComponentTypes())
            {
                var groupAttr = type.GetCustomAttribute<GroupAttribute>();
                if (groupAttr == null)
                    continue;

                if (!result.TryGetValue(groupAttr.Name, out var list))
                {
                    list = new List<Type>();
                    result[groupAttr.Name] = list;
                }
                list.Add(type);
            }

            return result;
        }

        private static HashSet<string> GetAllowedGroups(Type entityConfigType)
        {
            var tagAttr = entityConfigType.GetCustomAttribute<EntityTagAttribute>();
            if (tagAttr == null)
                return null;

            var allowed = new HashSet<string>();
            foreach (var groupType in tagAttr.GroupTypes)
            {
                var groupAttr = groupType.GetCustomAttribute<GroupAttribute>();
                if (groupAttr != null)
                    allowed.Add(groupAttr.Name);
                else
                    allowed.Add(DeriveGroupName(groupType));
            }
            return allowed;
        }

        private static string DeriveGroupName(Type groupType)
        {
            var name = groupType.Name;
            if (name.EndsWith("Group", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 5);
            return name;
        }

        public override VisualElement CreateInspectorGUI()
        {
            entityConfig = (EntityConfig)target;
            componentsProperty = serializedObject.FindProperty("components");

            var root = new VisualElement();

            // 使用 PropertyEditor 绘制 EntityConfig 自身的属性
            var selfElement = new StructedPropertyElement(entityConfig.GetType(), expandedInParent: true);
            selfElement.SetValue(entityConfig);
            root.Add(selfElement);

            // 分隔线
            root.Add(CreateDivider());

            // 组件列表容器
            componentContainer = new VisualElement();
            root.Add(componentContainer);
            RebuildComponentList();

            // 添加组件按钮
            var addButton = new Button(ShowAddComponentMenu);
            addButton.text = "Add Component";
            addButton.style.height = 24;
            addButton.style.marginTop = 8;
            root.Add(addButton);

            // 导出按钮
            var exportButton = new Button(() => entityConfig.Export());
            exportButton.text = "Export";
            exportButton.style.height = 24;
            exportButton.style.marginTop = 4;
            root.Add(exportButton);

            return root;
        }

        private static VisualElement CreateDivider()
        {
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            divider.style.marginTop = 4;
            divider.style.marginBottom = 4;
            return divider;
        }

        private void RebuildComponentList()
        {
            componentContainer.Clear();
            serializedObject.Update();

            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var element = componentsProperty.GetArrayElementAtIndex(i);
                var component = element.objectReferenceValue as ConfigComponent;
                int idx = i;

                if (component == null)
                {
                    var box = new VisualElement();
                    box.style.marginBottom = 2;
                    var warning = new HelpBox("Missing Component (null reference)", HelpBoxMessageType.Warning);
                    var removeBtn = new Button(() => RemoveComponent(idx));
                    removeBtn.text = "Remove";
                    box.Add(warning);
                    box.Add(removeBtn);
                    componentContainer.Add(box);
                    continue;
                }

                componentContainer.Add(CreateComponentElement(component, idx));
            }
        }

        private VisualElement CreateComponentElement(ConfigComponent component, int index)
        {
            var container = new VisualElement();
            container.style.marginBottom = 2;

            var displayName = GetComponentDisplayName(component);
            var foldout = new Foldout();
            foldout.text = displayName;
            foldout.value = true;

            var enableToggle = new Toggle();
            enableToggle.value = component.Enable;
            enableToggle.style.marginRight = 2;
            enableToggle.RegisterValueChangedCallback(evt =>
            {
                component.Enable = evt.newValue;
                Undo.RegisterCompleteObjectUndo(component, $"Toggle {displayName} {(component.Enable ? "Enable" : "Disable")}");
                EditorUtility.SetDirty(target);
            });

            var toggleRow = foldout.Q<Toggle>();
            toggleRow.style.borderTopWidth = 1;
            toggleRow.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            toggleRow.style.paddingTop = 2;
            toggleRow.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            var menuButton = new Button(() => ShowComponentMenu(component, index));
            menuButton.text = "";
            var menuIcon = new Image { image = EditorGUIUtility.IconContent("_Menu").image };
            menuIcon.style.width = 16;
            menuIcon.style.height = 16;
            menuButton.Add(menuIcon);
            menuButton.style.width = 20;
            menuButton.style.height = 20;
            menuButton.style.paddingLeft = 0;
            menuButton.style.paddingRight = 0;
            menuButton.style.paddingTop = 0;
            menuButton.style.paddingBottom = 0;
            menuButton.style.marginLeft = StyleKeyword.Auto;
            menuButton.style.backgroundColor = Color.clear;
            menuButton.style.borderLeftWidth = 0;
            menuButton.style.borderRightWidth = 0;
            menuButton.style.borderTopWidth = 0;
            menuButton.style.borderBottomWidth = 0;
            // hover/active 时显示边框
            menuButton.RegisterCallback<MouseEnterEvent>(_ =>
            {
                menuButton.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            });
            menuButton.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                menuButton.style.backgroundColor = Color.clear;
            });

            // Foldout -> Toggle -> VisualElement(含Label) 结构
            // 找到 Toggle 下直接包含 Label 的 VisualElement，在其前插入 enableToggle
            var lable = toggleRow.Q<Label>();
            lable.style.paddingLeft = 5;
            lable.parent.Insert(lable.parent.IndexOf(lable), enableToggle);
            toggleRow.Add(menuButton);

            var propertyElement = new StructedPropertyElement(component.GetType(), expandedInParent: true);
            propertyElement.SetValue(component);
            foldout.Add(propertyElement);

            container.Add(foldout);

            return container;
        }

        private static string GetComponentDisplayName(ConfigComponent component)
        {
            var type = component.GetType();
            var aliasAttr = type.GetCustomAttribute<AliasAttribute>();
            if (aliasAttr != null)
                return aliasAttr.Name;
            return ObjectNames.NicifyVariableName(type.Name);
        }

        private void ShowComponentMenu(ConfigComponent component, int index)
        {
            var menu = new GenericMenu();
            var idx = index;

            menu.AddItem(new GUIContent("Copy"), false, () => CopyComponent(component));
            menu.AddItem(new GUIContent("Paste"), false, () => { });

            // 收集 ContextMenu 标记的方法
            var contextMethods = GetContextMenuMethods(component.GetType());
            if (contextMethods.Count > 0)
            {
                menu.AddSeparator("");
                foreach (var (label, method) in contextMethods)
                {
                    var m = method;
                    menu.AddItem(new GUIContent(label), false, () => m.Invoke(component, null));
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Remove"), false, () => RemoveComponent(idx));
            menu.ShowAsContext();
        }

        private static List<(string label, MethodInfo method)> GetContextMenuMethods(Type type)
        {
            var result = new List<(string, MethodInfo)>();
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<ContextMenu>();
                if (attr == null) continue;
                if (method.GetParameters().Length != 0) continue;
                result.Add((attr.menuItem, method));
            }
            return result;
        }

        private void CopyComponent(ConfigComponent component)
        {
            var json = JsonUtility.ToJson(component);
            EditorGUIUtility.systemCopyBuffer = json;
        }

        private void ShowAddComponentMenu()
        {
            var groupedTypes = CollectGroupedComponentTypes();
            var allowedGroups = GetAllowedGroups(entityConfig.GetType());
            var menu = new GenericMenu();

            if (allowedGroups == null)
            {
                menu.AddDisabledItem(new GUIContent("No EntityTagAttribute - cannot add components"));
            }
            else
            {
                bool hasAny = false;
                foreach (var kvp in groupedTypes)
                {
                    if (!allowedGroups.Contains(kvp.Key))
                        continue;

                    hasAny = true;
                    foreach (var type in kvp.Value)
                    {
                        string displayName = $"{kvp.Key}/{ObjectNames.NicifyVariableName(type.Name)}";
                        var t = type;
                        menu.AddItem(new GUIContent(displayName), false, () => AddComponent(t));
                    }
                }

                if (!hasAny)
                    menu.AddDisabledItem(new GUIContent("No allowed components"));
            }

            if (groupedTypes.Count == 0)
                menu.AddDisabledItem(new GUIContent("No ConfigComponent types found"));

            menu.ShowAsContext();
        }

        private void AddComponent(Type componentType)
        {
            var component = CreateInstance(componentType) as ConfigComponent;
            component.name = componentType.Name;
            component.hideFlags = HideFlags.HideInHierarchy;

            Undo.RegisterCreatedObjectUndo(component, "Add Config Component");

            string assetPath = AssetDatabase.GetAssetPath(entityConfig);
            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.AddObjectToAsset(component, entityConfig);

            serializedObject.Update();
            componentsProperty.arraySize++;
            componentsProperty.GetArrayElementAtIndex(componentsProperty.arraySize - 1)
                .objectReferenceValue = component;
            serializedObject.ApplyModifiedProperties();

            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath);
            }

            RebuildComponentList();
        }

        private void RemoveComponent(int index)
        {
            serializedObject.Update();
            if (index < 0 || index >= componentsProperty.arraySize)
                return;

            var component = componentsProperty.GetArrayElementAtIndex(index).objectReferenceValue;

            // SerializedProperty: 非空引用需要先置空再删除
            if (component != null)
                componentsProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            componentsProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();

            if (component != null)
                Undo.DestroyObjectImmediate(component);

            string assetPath = AssetDatabase.GetAssetPath(entityConfig);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath);
            }

            RebuildComponentList();
        }
    }
}
