using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(LayerSelectAttribute))]
    internal class LayerSelectElement : TAttributePropertyElement<int, LayerSelectAttribute>
    {
        private readonly LayerField field = new LayerField();
        public LayerSelectElement()
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

        protected override void SetValueToField()
        {
            field.value = value;
        }
    }
}
