using System.Reflection;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class FieldEditor
    {
        public StructedFieldEditor Parent;
        public FieldInfo Field;
        public void OnValueChange(object newValue)
        {
            Parent.OnFieldChange(Field, newValue);
        }

        public VisualElement CreateEditorElement()
        {
            var value = Parent.GetFieldValue(Field);
            //临时测试代码
            var f = new IntegerField();
            f.SetValueWithoutNotify((int)value);
            f.RegisterValueChangedCallback(evt =>
            {
                OnValueChange(evt.newValue);
            });
            return f;
        }
    }
}
