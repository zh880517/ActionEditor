using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(bool))]
    public class BoolElement : TPropertyElement<bool>
    {
        protected readonly Toggle field = new Toggle();
        public override bool ReadOnly { get => field.enabledSelf == false; set => field.SetEnabled(!value); }
        public BoolElement()
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
            field.SetValueWithoutNotify((bool)value);
        }

        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
