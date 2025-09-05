using System;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(EnumFlagAttribute))]
    public class EnumFlagElement : TAttributePropertyElement<Enum, EnumFlagAttribute>
    {
        protected readonly UnityEditor.UIElements.EnumFlagsField field = new UnityEditor.UIElements.EnumFlagsField();
        public override bool ReadOnly { get => field.enabledSelf == false; set => field.SetEnabled(!value); }
        public EnumFlagElement()
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
            field.SetValueWithoutNotify((Enum)value);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
