using PropertyEditor.BuiltIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PropertyEditor
{
    public static class PropertyElementFactory
    {
        struct PropertyEditorUnit
        {
            public Type EditorElementType;
            public int Priority;
        }
        private static Dictionary<Type, PropertyEditorUnit> elementCache;

        private static void Initialize()
        {
            if (elementCache != null)
                return;
            elementCache = new Dictionary<Type, PropertyEditorUnit>();
            var types = TypeWithAttributeCollector<PropertyElement, CustomPropertyElementAttribute>.Collector();
            foreach (var kv in types)
            {
                if (elementCache.TryGetValue(kv.Key, out var unit))
                {
                    if (unit.Priority > kv.Value.Priority)
                        continue;
                }
                elementCache[kv.Value.DataType] = new PropertyEditorUnit()
                {
                    EditorElementType = kv.Key,
                    Priority = kv.Value.Priority
                };
            }
        }

        private static Type GetEditorElementType(Type dataType)
        {
            Initialize();
            if (dataType == null)
                return null;
            if (elementCache.TryGetValue(dataType, out var unit))
            {
                return unit.EditorElementType;
            }
            if (dataType.IsEnum)
                return typeof(EnumElement);
            if (dataType.IsSubclassOf(typeof(UnityEngine.Object)))
                return typeof(ObjectElement);

            return null;
        }

        private static PropertyElement CreateArrayPropertyElement(FieldInfo field)
        {
            Type elementType = field.FieldType.GetElementType();
            var arrayElement = new ArrayPropertyElement(elementType) { Field = field };
            if (field.IsDefined(typeof(FixedArraySizeAttribute)))
            {
                arrayElement.SetResizeable(false);
            }
            return arrayElement;
        }
        private static PropertyElement CreateListPropertyElement(FieldInfo field)
        {
            Type elementType = field.FieldType.GetGenericArguments()[0];
            var listElement = new ListPropertyElement(elementType) { Field = field };
            if (field.IsDefined(typeof(FixedArraySizeAttribute)))
            {
                listElement.SetResizeable(false);
            }
            return listElement;
        }

        public static PropertyElement CreateByFieldInfo(FieldInfo field)
        {
            if (field == null)
                return null;
            if (field.IsDefined(typeof(NonSerializedAttribute)))
                return null;
            if (field.IsDefined(typeof(UnityEngine.HideInInspector)))
                return null;
            if (field.IsDefined(typeof(HiddenInPropertyEditor)))
                return null;
            var customAttr = field.GetCustomAttribute<CustomPropertyAttribute>();
            if (customAttr != null)
            {
                var customEditorType = GetEditorElementType(customAttr.GetType());
                if (customEditorType != null)
                {
                    if (Activator.CreateInstance(customEditorType) is PropertyElement editor)
                    {
                        editor.Field = field;
                        (editor as AttributePropertyElement).SetAttribute(customAttr);
                        editor.OnCreate();
                        return editor;
                    }
                }
            }
            if(field.FieldType.IsArray)
                return CreateArrayPropertyElement(field);
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                return CreateListPropertyElement(field);
            var editorType = GetEditorElementType(field.FieldType);
            if (editorType != null)
            {
                if (Activator.CreateInstance(editorType) is PropertyElement editor)
                {
                    editor.Field = field;
                    editor.OnCreate();
                    return editor;
                }
            }
            if (field.FieldType.IsClass || (field.FieldType.IsValueType && !field.FieldType.IsPrimitive))
            {
                bool expanded = field.IsDefined(typeof(ExpandedInParentAttribute));
                var structedEditor = new StructedPropertyElement(field.FieldType, expanded);
                structedEditor.Field = field;
                return structedEditor;
            }

            return null;
        }

        public static PropertyElement CreateByUnityObject(UnityEngine.Object obj)
        {
            var type = obj?.GetType();
            if (type == null)
                return null;
            var editor = new StructedPropertyElement(type);
            return editor;
        }

        public static PropertyElement CreateByType(Type type, bool expandedInParent = false)
        {
            if (type == null)
                return null;
            if (type.IsArray)
                return new ArrayPropertyElement(type.GetElementType());
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return new ListPropertyElement(type.GetGenericArguments()[0]);

            var editorType = GetEditorElementType(type);
            if (editorType != null)
            {
                if (Activator.CreateInstance(editorType) is PropertyElement editor)
                {
                    editor.OnCreate();
                    return editor;
                }
            }
            if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
            {
                var structedEditor = new StructedPropertyElement(type, expandedInParent);
                return structedEditor;
            }
            return null;
        }
    }
}
