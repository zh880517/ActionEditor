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
            container.style.position = Position.Relative;
            container.style.marginBottom = 2;

            // 使用 Foldout 展示组件
            var foldout = new Foldout();
            foldout.text = ObjectNames.NicifyVariableName(component.GetType().Name);
            foldout.value = true;

            // 组件属性编辑器（展开在 Foldout 内，不带自身 Foldout）
            var propertyElement = new StructedPropertyElement(component.GetType(), expandedInParent: true);
            propertyElement.SetValue(component);
            foldout.Add(propertyElement);

            // 移除按钮（绝对定位在右上角）
            int idx = index;
            var removeButton = new Button(() => RemoveComponent(idx));
            removeButton.text = "✕";
            removeButton.style.position = Position.Absolute;
            removeButton.style.right = 2;
            removeButton.style.top = 2;
            removeButton.style.width = 18;
            removeButton.style.height = 16;
            removeButton.style.fontSize = 10;
            removeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            removeButton.style.paddingLeft = 0;
            removeButton.style.paddingRight = 0;
            removeButton.style.paddingTop = 0;
            removeButton.style.paddingBottom = 0;

            container.Add(foldout);
            container.Add(removeButton);

            return container;
        }

        private void ShowAddComponentMenu()
        {
            var types = CollectComponentTypes();
            var menu = new GenericMenu();

            foreach (var type in types)
            {
                string displayName = ObjectNames.NicifyVariableName(type.Name);
                var t = type;
                menu.AddItem(new GUIContent(displayName), false, () => AddComponent(t));
            }

            if (types.Count == 0)
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
