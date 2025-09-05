using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(Vector3Int))]
    public class Vector3IntElement : TPropertyElement<Vector3Int>
    {
        private readonly Vector3IntField field = new Vector3IntField();
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
        }
        public Vector3IntElement()
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
