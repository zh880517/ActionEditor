using System;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(IntPopupSelectAttribute))]
    public class IntPopupSelectElement : TAttributePropertyElement<int, IntPopupSelectAttribute>
    {
        private readonly PopupField<string> field =  new PopupField<string>();

        public IntPopupSelectElement()
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
            if (value < 0 || value >= attribute.Options.Length)
            {
                field.value = attribute.Options[0];
            }
            else
            {
                field.value = attribute.Options[value];
            }
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            if (isReadOnly)
            {
                SetValueToField();
                return;
            }
            int index = Array.IndexOf(attribute.Options, evt.newValue);
            if (index >= 0)
            {
                value = index;
                using var e = PropertyValueChangedEvent.GetPooled(this, value, Field, Index);
                SendEvent(e);
            }
        }
    }
}
