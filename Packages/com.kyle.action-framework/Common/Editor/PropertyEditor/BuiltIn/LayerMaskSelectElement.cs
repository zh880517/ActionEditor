using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(LayerMaskSelectAttribute))]
    public class LayerMaskSelectElement : TAttributePropertyElement<int, LayerMaskSelectAttribute>
    {
        private readonly LayerMaskField field = new LayerMaskField();
        public LayerMaskSelectElement()
        {
            Add(field);
            field.RegisterValueChangedCallback(OnValueChanged);
        }
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public override void SetLable(string name, string tip)
        {
            field.label = name;
            if (!string.IsNullOrEmpty(tip))
                field.tooltip = tip;
        }
        public override void SetLableWidth(float width)
        {
            field.labelElement.style.minWidth = width;
        }
        protected override void SetValueToField()
        {
            field.value = value;
        }
    }

    [CustomPropertyElement(typeof(LayerMask))]
    public class LayerMaskElement : PropertyElement
    {
        private readonly LayerMaskField field = new LayerMaskField();
        public LayerMaskElement()
        {
            Add(field);
            field.RegisterValueChangedCallback(OnValueChanged);
        }
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public override void SetLable(string name, string tip)
        {
            field.label = name;
            if (!string.IsNullOrEmpty(tip))
                field.tooltip = tip;
        }
        public override void SetLableWidth(float width)
        {
            field.labelElement.style.minWidth = width;
        }
        public override void SetValue(object value)
        {
            field.value = ((LayerMask)value).value;
        }

        private void OnValueChanged(ChangeEvent<int> evt)
        {
            evt.StopPropagation();
            LayerMask v = evt.newValue;
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
                SendEvent(e);
        }
    }
}
