using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(Rect))]
    public class RectElement : TPropertyElement<Rect>
    {
        protected readonly RectField field = new RectField();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public RectElement()
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
            field.SetValueWithoutNotify((Rect)value);
        }
        protected override void SetValueToField()
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
