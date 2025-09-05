using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(Color))]
    public class ColorElement : TPropertyElement<Color>
    {
        private readonly ColorField field = new ColorField();
        public override bool ReadOnly { get => field.enabledSelf == false; set => field.SetEnabled(!value); }
        public ColorElement()
        {
            Add(field);
            field.RegisterValueChangedCallback(OnValueChanged);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
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
    }
}
