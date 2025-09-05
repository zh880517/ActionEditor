using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    public class ObjectElement : TPropertyElement<Object>
    {
        protected readonly ObjectField field = new ObjectField();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public ObjectElement()
        {
            field.RegisterValueChangedCallback(OnValueChanged);
            Add(field);
        }

        public override void OnCreate()
        {
            if (Field != null)
            {
                field.objectType = Field.FieldType;
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
            field.SetValueWithoutNotify((Object)value);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
