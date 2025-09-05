using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(AnimationCurve))]
    public class CurveElement : TPropertyElement<AnimationCurve>
    {
        private readonly CurveField field = new CurveField();
        public override bool ReadOnly { get => field.enabledSelf == false; set => field.SetEnabled(!value); }
        public CurveElement()
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

        protected override void OnValueChanged(ChangeEvent<AnimationCurve> evt)
        {
            evt.StopPropagation();

            value ??= new AnimationCurve();
            value.keys = evt.newValue.keys;
            value.preWrapMode = evt.newValue.preWrapMode;
            value.postWrapMode = evt.newValue.postWrapMode;
            using var e = PropertyValueChangedEvent.GetPooled(this, value, Field, Index);
            SendEvent(e);

        }
    }
}
