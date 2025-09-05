using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(Vector3))]
    public class Vector3Element : TPropertyElement<Vector3>
    {
        private readonly Vector3Field field = new Vector3Field();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public Vector3Element()
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
