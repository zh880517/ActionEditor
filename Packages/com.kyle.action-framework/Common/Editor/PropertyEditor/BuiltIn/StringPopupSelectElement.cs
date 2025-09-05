using System;
using System.Linq;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(StringPopupSelectAttribute))]
    public class StringPopupSelectElement : TAttributePropertyElement<string, StringPopupSelectAttribute>
    {
        private readonly PopupField<string> field = new PopupField<string>();
        public StringPopupSelectElement()
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
        public override void OnCreate()
        {
            if (attribute != null && attribute.Options != null)
            {
                field.choices = new System.Collections.Generic.List<string>(attribute.Options);
            }
        }

        protected override void SetValueToField()
        {
            if (attribute == null || attribute.Options == null || attribute.Options.Length == 0)
                return;
            if (!attribute.Options.Contains(value))
            {
                field.value = attribute.Options[0];
            }
            else
            {
                field.value = value;
            }
        }
    }
}
