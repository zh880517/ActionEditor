using System.Reflection;
using UnityEditor;

namespace Flow.EditorView
{
    public class StructedFieldEditor
    {
        public StructedFieldEditor Parent;
        public FieldInfo Field;
        public object Value;

        private void BeforeValueChange()
        {
            if (Value is UnityEngine.Object @object)
            {
                Undo.RegisterCompleteObjectUndo(@object, "Modify Value");
            }
            else
            {
                Parent?.BeforeValueChange();
            }
        }

        public void OnFieldChange(FieldInfo field, object value)
        {
            BeforeValueChange();
            field.SetValue(Value, value);
            if(Parent != null && Field.FieldType.IsValueType)
            {
                Parent.OnFieldChange(Field, Value);
                RefreshValue();
            }
        }

        public object GetFieldValue(FieldInfo field)
        {
            return field.GetValue(Value);
        }

        public void RefreshValue()
        {
            if (Parent != null)
            {
                Value = Parent.GetFieldValue(Field);
            }
        }
    }
}
