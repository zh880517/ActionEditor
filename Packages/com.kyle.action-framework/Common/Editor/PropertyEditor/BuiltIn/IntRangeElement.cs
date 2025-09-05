using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(IntegerRangeAttribute))]
    public class IntRangeElement : TAttributePropertyElement<int, IntegerRangeAttribute>
    {
        private readonly SliderInt field = new SliderInt();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public IntRangeElement()
        {
            field.RegisterValueChangedCallback(OnValueChanged);
            Add(field);
        }
        public override void OnCreate()
        {
            if (attribute != null)
            {
                field.lowValue = attribute.Min;
                field.highValue = attribute.Max;
            }
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
            field.SetValueWithoutNotify((int)value);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
