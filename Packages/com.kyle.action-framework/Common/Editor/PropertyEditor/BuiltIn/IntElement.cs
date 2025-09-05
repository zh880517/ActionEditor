using UnityEngine.UIElements;

namespace PropertyEditor.BuiltIn
{
    public abstract class IntegerElement : PropertyElement
    {
        protected readonly LongField field = new LongField();
        public IntegerElement()
        {
            field.RegisterValueChangedCallback(OnValueChanged);
            Add(field);
        }
        public override bool ReadOnly
        {
            get => field.enabledSelf;
            set => field.SetEnabled(!value);
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
            field.SetValueWithoutNotify((long)value);
        }

        protected virtual void OnValueChanged(ChangeEvent<long> evt)
        {
            evt.StopPropagation();
            using var e = PropertyValueChangedEvent.GetPooled(this, evt.newValue, Field, Index);
            SendEvent(e);
        }
    }

    [CustomPropertyElement(typeof(long))]
    public class LongElement : IntegerElement
    {
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((long)value);
        }

        protected override void OnValueChanged(ChangeEvent<long> evt)
        {
            evt.StopPropagation();
            using var e = PropertyValueChangedEvent.GetPooled(this, evt.newValue, Field, Index);
            SendEvent(e);
        }
    }

    [CustomPropertyElement(typeof(ulong))]
    public class UlongElement : IntegerElement
    {
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((int)value);
        }

        protected override void OnValueChanged(ChangeEvent<long> evt)
        {
            evt.StopPropagation();
            ulong v = (ulong)evt.newValue;
            if (v < 0)
            {
                v = 0;
                field.SetValueWithoutNotify(0);
            }
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
            SendEvent(e);
        }
    }

    [CustomPropertyElement(typeof(int))]
    public class IntElement : IntegerElement
    {
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((int)value);
        }

        protected override void OnValueChanged(ChangeEvent<long> evt)
        {
            evt.StopPropagation();
            int v = (int)System.Math.Clamp(evt.newValue, int.MinValue, int.MaxValue);
            if (v != evt.newValue)
            {
                field.SetValueWithoutNotify(v);
            }
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
            SendEvent(e);
        }
    }

    [CustomPropertyElement(typeof(uint))]
    public class UIntElement : IntegerElement
    {
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((int)value);
        }

        protected override void OnValueChanged(ChangeEvent<long> evt)
        {
            evt.StopPropagation();
            uint v = (uint)System.Math.Clamp(evt.newValue, uint.MinValue, uint.MaxValue);
            if (v != evt.newValue)
            {
                field.SetValueWithoutNotify(v);
            }
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
            SendEvent(e);
        }
    }
    [CustomPropertyElement(typeof(short))]
    public class ShortElement : IntegerElement
    {
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((short)value);
        }

        protected override void OnValueChanged(ChangeEvent<long> evt)
        {
            evt.StopPropagation();
            short v = (short)System.Math.Clamp(evt.newValue, short.MinValue, short.MaxValue);
            if (v != evt.newValue)
            {
                field.SetValueWithoutNotify(v);
            }
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
            SendEvent(e);
        }
    }
    [CustomPropertyElement(typeof(ushort))]
    public class UShortElement : IntegerElement
    {
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((ushort)value);
        }

        protected override void OnValueChanged(ChangeEvent<long> evt)
        {
            evt.StopPropagation();
            ushort v = (ushort)System.Math.Clamp(evt.newValue, ushort.MinValue, ushort.MaxValue);
            if (v != evt.newValue)
            {
                field.SetValueWithoutNotify(v);
            }
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
            SendEvent(e);
        }
    }
}
