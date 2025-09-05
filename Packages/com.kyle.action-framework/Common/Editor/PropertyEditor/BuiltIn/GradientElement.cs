using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(Gradient))]
    public class GradientElement : TPropertyElement<Gradient>
    {
        protected readonly GradientField field = new GradientField();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public GradientElement()
        {
            field.RegisterValueChangedCallback(OnValueChanged);
            Add(field);
        }
        public override void SetLable(string name, string tip)
        {
            field.label = name;
            field.tooltip = tip;
        }
        public override void SetLableWidth(float width)
        {
            field.labelElement.style.minWidth = width;
        }
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((Gradient)value);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }

        protected override void OnValueChanged(ChangeEvent<Gradient> evt)
        {
            evt.StopPropagation();
            if (isReadOnly)
            {
                SetValueToField();
                return;
            }
            value ??= new Gradient();
            value.colorKeys = evt.newValue.colorKeys;
            value.alphaKeys = evt.newValue.alphaKeys;
            value.mode = evt.newValue.mode;
            using var e = PropertyValueChangedEvent.GetPooled(this, value, Field, Index);
            SendEvent(e);
        }
    }
}
