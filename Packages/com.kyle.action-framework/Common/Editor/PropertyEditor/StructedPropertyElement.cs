using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

namespace PropertyEditor
{
    public class StrctedFieldElement : VisualElement
    {
        public PropertyElement Element { get; private set; }
        public bool ReadOnly { get; private set; }
        public string DisplayName { get; private set; }
        public string ToolTip { get; private set; }

        public StrctedFieldElement()
        {
            style.flexDirection = FlexDirection.Row;
        }

        public void Initialize(PropertyElement element, bool readOnly, string displayName, string toolTip)
        {
            Element = element;
            ReadOnly = readOnly;
            DisplayName = displayName;
            ToolTip = toolTip;
            style.flexDirection = FlexDirection.Row;
            Element.SetLable(displayName, toolTip);
            Element.style.flexGrow = 1;
            Add(Element);
        }
    }

    public class StructedPropertyElement : PropertyElement
    {
        public struct FieldUnit
        {
            public PropertyElement Element;
            public bool ReadOnly;//属性标记
        }

        public const float TitleIndent = 10f;//缩进,系统默认Lable最小宽度是120
        protected List<StrctedFieldElement> children = new List<StrctedFieldElement>();
        protected Foldout foldout;
        protected bool readOnly = false;
        protected string lable;
        protected string toolTip;
        protected object value;
        public override bool ReadOnly 
        { 
            get => readOnly; 
            set
            {
                if (readOnly != value)
                {
                    readOnly = value;
                    foreach (var item in children)
                    {
                        if(!item.ReadOnly)
                        {
                            item.Element.ReadOnly = value;
                        }
                    }
                }
            } 
        }
        public bool ExpandedInParent => foldout != null;

        public StructedPropertyElement(Type type, bool expandedInParent = false)
        {
            SetExpandedInParent(expandedInParent);
            List<FieldInfo> fields = new List<FieldInfo>();
            var currentType = type;
            while (currentType != null)
            {
                if (currentType == typeof(object))
                    break;
                var typeFields = currentType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                fields.InsertRange(0, typeFields);
                currentType = currentType.BaseType;
            }
            foreach (var field in fields)
            {
                PropertyElement element = PropertyElementFactory.CreateByFieldInfo(field);
                if (element != null)
                {
                    var child = new StrctedFieldElement();
                    string displayName = field.Name;
                    string toolTip = null;
                    var display = field.GetCustomAttribute<DisplayAttribute>();
                    if (display != null)
                    {
                        displayName = display.Name;
                        toolTip = display.Tooltip;
                    }
                    else
                    {
                        var displayNameAttr = field.GetCustomAttribute<DisplayNameAttribute>();
                        if (displayNameAttr != null)
                        {
                            displayName = displayNameAttr.DisplayName;
                            toolTip = displayNameAttr.Tooltip;
                        }
                    }
                    child.Initialize(element, readOnly || field.IsDefined(typeof(ReadOnlyAttribute)), displayName, toolTip);
                    element.ReadOnly = readOnly || child.ReadOnly;
                    children.Add(child);

                    if (foldout != null)
                        foldout.Add(element);
                    else
                        Add(element);
                }
            }

            RegisterCallback<PropertyValueChangedEvent>(OnPropertyValueChangedEvent);
            if(type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                RegisterCallback<RegisterUndoEvent>(OnRegisterUndoEvent);
            }
        }

        public void SetExpandedInParent(bool expanded)
        {
            if (expanded == (foldout == null))
                return;
            if(!expanded)
            {
                foldout = new Foldout();
                foldout.text = lable;
                foldout.tooltip = toolTip;
                Add(foldout);

                foreach (var item in children)
                {
                    foldout.Add(item.Element);
                }
            }
            else
            {
                foldout.RemoveFromHierarchy();
                foldout = null;
                foreach (var item in children)
                {
                    Add(item.Element);
                }
            }
        }

        public override void SetLable(string name, string tip)
        {
            lable = name;
            toolTip = tip;
            if (foldout != null)
            {
                foldout.text = name;
                foldout.tooltip = tip;
            }
        }

        public override void SetLableWidth(float width)
        {
            foreach (var item in children)
            {
                item.Element.SetLableWidth(width);
            }
        }

        public override void SetValue(object value)
        {
            this.value = value;
            foreach (var item in children)
            {
                var v = item.Element.Field.GetValue(value);
                item.Element.SetValue(v);
            }
        }

        public override PropertyElement Find(string name)
        {
            foreach (var item in children)
            {
                if (item.Element.Field.Name == name)
                {
                    return item.Element;
                }
            }
            return null;
        }

        public StrctedFieldElement FindChild(string name)
        {
            foreach (var item in children)
            {
                if (item.Element.Field.Name == name)
                {
                    return item;
                }
            }
            return null;
        }
        public StrctedFieldElement FindByPath(string[] path)
        {
            StrctedFieldElement current = null;
            for (int i = 0; i < path.Length; i++)
            {
                if (i == 0)
                {
                    current = FindChild(path[i]);
                }
                else
                {
                    if (current == null)
                        return null;
                    if (current.Element is StructedPropertyElement structed)
                    {
                        current = structed.FindChild(path[i]);
                    }
                    else
                    {
                        return null;
                    }
                }
                if (current == null)
                    return null;
            }
            return current;
        }

        private void OnPropertyValueChangedEvent(PropertyValueChangedEvent evt)
        {
            if (evt.target == evt.currentTarget)
                return;
            evt.StopPropagation();
            if(value is UnityEngine.Object obj)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(obj, "Modify Property");
            }
            else
            {
                using var e = RegisterUndoEvent.GetPooled(this, "Modify Property");
                SendEvent(e);
            }
            if(evt.Field != null)
            {
                evt.Field.SetValue(value, evt.Value);
                if(Field != null && Field.FieldType.IsValueType)
                {
                    using var e = PropertyValueChangedEvent.GetPooled(this, value, Field, Index);
                    SendEvent(e);
                }
            }
        }

        private void OnRegisterUndoEvent(RegisterUndoEvent evt)
        {
            if (evt.target == evt.currentTarget)
                return;
            if (value is UnityEngine.Object obj)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(obj, evt.ActionName);
                evt.StopPropagation();
            }
        }

    }
}
