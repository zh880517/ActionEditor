using UnityEngine;
using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    [CustomPropertyElement(typeof(Quaternion))]
    public class QuaternionElement : Vector3Element
    {
        public override void SetValue(object value)
        {
            Quaternion v = (Quaternion)value;
            base.SetValue(v.eulerAngles);
        }

        protected override void OnValueChanged(ChangeEvent<Vector3> evt)
        {
            evt.StopPropagation();
            if (isReadOnly)
            {
                SetValueToField();
                return;
            }
            var v = Quaternion.Euler(value);
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
            SendEvent(e);
        }
    }
}
