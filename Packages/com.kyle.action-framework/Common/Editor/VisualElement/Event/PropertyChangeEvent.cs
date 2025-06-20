using UnityEditor;
using UnityEngine.UIElements;

public class PropertyChangeEvent : EventBase<PropertyChangeEvent>
{
    public SerializedProperty Property { get; private set; }

    public static PropertyChangeEvent GetPooled(SerializedProperty property)
    {
        var evt = GetPooled();
        evt.Property = property;
        evt.bubbles = true; // 设置事件可以冒泡
        return evt;
    }
}
