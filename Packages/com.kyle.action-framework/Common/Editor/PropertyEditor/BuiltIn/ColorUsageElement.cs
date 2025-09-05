using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(UnityEngine.ColorUsageAttribute))]
    public class ColorUsageElement : TAttributePropertyElement<UnityEngine.Color, UnityEngine.ColorUsageAttribute>
    {
        protected readonly UnityEditor.UIElements.ColorField field = new UnityEditor.UIElements.ColorField();
        public override bool ReadOnly { get => field.enabledSelf == false; set => field.SetEnabled(!value); }
        public ColorUsageElement()
        {
            field.RegisterValueChangedCallback(OnValueChanged);
            Add(field);
        }
        public override void OnCreate()
        {
            if (attribute != null)
            {
                field.showAlpha = attribute.showAlpha;
                field.hdr = attribute.hdr;
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
            field.SetValueWithoutNotify((UnityEngine.Color)value);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
