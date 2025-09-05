using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

namespace PropertyEditor
{
    public class StructedPropertyElement : PropertyElement
    {
        public struct FieldUnit
        {
            public PropertyElement Element;
            public bool ReadOnly;//属性标记
        }

        public const float TitleIndent = 10f;//缩进
        protected List<FieldUnit> children = new List<FieldUnit>();
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
                    var unit = new FieldUnit 
                    { 
                        Element = element,
                        ReadOnly = field.IsDefined(typeof(ReadOnlyAttribute)),
                    };
                    element.ReadOnly = readOnly || unit.ReadOnly;
                    element.Field = field;
                    var display = field.GetCustomAttribute<DisplayAttribute>();
                    if (display != null)
                    {
                        element.SetLable(display.Name, display.Tooltip);
                    }
                    else
                    {
                        element.SetLable(field.Name, null);
                    }
                    children.Add(unit);

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
                foldout.contentContainer.style.paddingLeft = TitleIndent;
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
                item.Element.SetLableWidth(width - TitleIndent);
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
            if (value is UnityEngine.Object obj)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(obj, evt.ActionName);
                evt.StopPropagation();
            }
        }

    }
}
