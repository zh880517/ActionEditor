using System.Reflection;
using UnityEngine.UIElements;

namespace PropertyEditor
{
    public class PropertyValueChangedEvent : EventBase<PropertyValueChangedEvent>
    {
        public object Value { get; private set; }
        public FieldInfo Field { get; private set; }//for field
        public int Index { get; private set; }//for array element
        protected override void Init()
        {
            base.Init();
            bubbles = true;
        }

        public static PropertyValueChangedEvent GetPooled(PropertyElement target, object value, FieldInfo field, int index = 0)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.Value = value;
            evt.Field = field;
            evt.Index = index;
            return evt;
        }
    }
}
