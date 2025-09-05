using System;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    public class EnumElement : TPropertyElement<Enum>
    {
        private readonly EnumField field;
        public override bool ReadOnly { get => field.enabledSelf == false; set => field.SetEnabled(!value); }
        public EnumElement()
        {
            field = new EnumField();
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
