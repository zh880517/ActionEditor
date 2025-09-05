using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(float))]
    public class FloatElement : TPropertyElement<float>
    {
        private readonly FloatField field = new FloatField();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public FloatElement() 
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

        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
