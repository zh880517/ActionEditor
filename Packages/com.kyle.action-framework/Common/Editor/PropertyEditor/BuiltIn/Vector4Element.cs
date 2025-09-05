using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(Vector4))]
    public class Vector4Element : TPropertyElement<Vector4>
    {
        private readonly Vector4Field field = new Vector4Field();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public Vector4Element()
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
