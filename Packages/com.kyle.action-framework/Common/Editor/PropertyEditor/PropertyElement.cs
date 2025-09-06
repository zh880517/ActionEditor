using System.Reflection;
using UnityEngine.UIElements;

namespace PropertyEditor
{
    public abstract class PropertyElement : VisualElement
    {
        public const float LabelMinWidth = 120;
        public FieldInfo Field;//for field
        public int Index = -1;//for array element
        public abstract bool ReadOnly { get; set; }
        public abstract void SetLable(string name, string tip);
        public abstract void SetLableWidth(float width);
        public abstract void SetValue(object value);
        public virtual PropertyElement Find(string name) =>null;

        public virtual void OnCreate() { }//called after set Field and Index
    }

    public abstract class AttributePropertyElement : PropertyElement
    {
        public abstract void SetAttribute(System.Attribute attribute);
    }


    public abstract class TPropertyElement<T> : PropertyElement
    {
        protected T value;
        protected bool isReadOnly = false;
        public override bool ReadOnly
        {
            get => isReadOnly;
            set => isReadOnly = value;
        }
        protected abstract void SetValueToField();
        public override void SetValue(object value)
        {
            this.value = (T)value;
            SetValueToField();
        }
        protected virtual void OnValueChanged(ChangeEvent<T> evt) 
        {
            evt.StopPropagation();
            if (isReadOnly)
            {
                SetValueToField();
                return;
            }
            value = evt.newValue;
            using var e = PropertyValueChangedEvent.GetPooled(this, value, Field, Index);
            SendEvent(e);
        }
    }

    public abstract class TAttributePropertyElement<T, A> : AttributePropertyElement where A : System.Attribute
    {
        protected A attribute;
        public override void SetAttribute(System.Attribute attribute)
        {
            this.attribute = attribute as A;
        }
        protected T value;
        protected bool isReadOnly = false;
        public override bool ReadOnly
        {
            get => isReadOnly;
            set => isReadOnly = value;
        }
        protected abstract void SetValueToField();
        public override void SetValue(object value)
        {
            this.value = (T)value;
            SetValueToField();
        }
        protected virtual void OnValueChanged(ChangeEvent<T> evt)
        {
            evt.StopPropagation();
            if (isReadOnly)
            {
                SetValueToField();
                return;
            }
            value = evt.newValue;
            using var e = PropertyValueChangedEvent.GetPooled(this, value, Field, Index);
            SendEvent(e);
        }
    }
}
