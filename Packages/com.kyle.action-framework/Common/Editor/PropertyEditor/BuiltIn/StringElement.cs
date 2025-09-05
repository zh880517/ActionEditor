using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(string))]
    public class StringElement : TPropertyElement<string>
    {
        protected readonly TextField field = new TextField();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public StringElement()
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
            field.SetValueWithoutNotify((string)value);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
