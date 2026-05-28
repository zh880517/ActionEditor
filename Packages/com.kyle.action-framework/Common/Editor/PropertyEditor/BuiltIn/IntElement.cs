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
    public class UlongElement : PropertyElement
    {
        private readonly TextField field = new TextField();
        private bool readOnly;
        private ulong value;

        public UlongElement()
        {
            field.RegisterValueChangedCallback(OnValueChanged);
            Add(field);
        }

        public override bool ReadOnly
        {
            get => readOnly;
            set
            {
                readOnly = value;
                field.SetEnabled(!value);
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
            this.value = value is ulong v ? v : 0UL;
            field.SetValueWithoutNotify(this.value.ToString());
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            if (!ulong.TryParse(evt.newValue, out var v))
            {
                field.SetValueWithoutNotify(value.ToString());
                return;
            }
            value = v;
            using var e = PropertyValueChangedEvent.GetPooled(this, v, Field, Index);
            SendEvent(e);
        }
    }

    [CustomPropertyElement(typeof(int))]
    public class IntElement : IntegerElement
    {
        public override void SetValue(object value)
        {
            field.SetValueWithoutNotify((long)(int)value);
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
            field.SetValueWithoutNotify((long)(uint)value);
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
            field.SetValueWithoutNotify((long)(short)value);
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
            field.SetValueWithoutNotify((long)(ushort)value);
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
