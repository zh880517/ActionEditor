using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

internal class DelayInputDrawer : TCombinableDrawer<DelayInputAttribute>
{
    protected override void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, DelayInputAttribute attribute)
    {
        if (TrySet<TextField, string>(field))
            return;
        if (TrySet<IntegerField, int>(field))
            return;
        if (TrySet<FloatField, float>(field))
            return;
        if (TrySet<DoubleField, double>(field))
            return;
        if (TrySet<LongField, long>(field))
            return;
        if (TrySet<UnsignedIntegerField, uint>(field))
            return;
        if (TrySet<UnsignedLongField, ulong>(field))
            return;
    }

    private bool TrySet<T, TValue>(PropertyField field) where T : TextInputBaseField<TValue>
    {
        var e = field.Q<T>();
        if (e != null)
        {
            e.isDelayed = true;
            return true;
        }
        return false;
    }
}
