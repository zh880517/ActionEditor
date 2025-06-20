using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public abstract class CombinableDrawer
{
    public abstract System.Type AttribteType { get; }
    public abstract void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, CombinableProertyAttribute attribute);
}

public abstract class TCombinableDrawer<T> : CombinableDrawer where T : CombinableProertyAttribute
{
    public override System.Type AttribteType => typeof(T);
    public override void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, CombinableProertyAttribute attribute)
    {
        OnCreatePropertyGUI(field, property, attribute as T);
    }

    protected abstract void OnCreatePropertyGUI(PropertyField field, SerializedProperty property, T attribute);
}

[CustomPropertyDrawer(typeof(CombinedAttribute))]
public class CombinedDrawer : PropertyDrawer
{   
    private static Dictionary<System.Type, CombinableDrawer> drawers;
    private static readonly Dictionary<FieldInfo, CombinableProertyAttribute[]> fieldAttributes = new Dictionary<FieldInfo, CombinableProertyAttribute[]>();

    public static CombinableDrawer GetDrawer(System.Type type)
    {
        if(drawers == null)
        {
            drawers = new Dictionary<System.Type, CombinableDrawer>();
            foreach (var item in TypeCollector<CombinableDrawer>.Types)
            {
                var instance = System.Activator.CreateInstance(item) as CombinableDrawer;
                drawers[instance.AttribteType] = instance;
            }
        }
        return drawers.TryGetValue(type, out var drawer) ? drawer : null;
    }

    public static CombinableProertyAttribute[] GetAttributes(FieldInfo fieldInfo)
    {
        if(!fieldAttributes.TryGetValue(fieldInfo, out var attributes))
        {
            attributes = fieldInfo.GetCustomAttributes<CombinableProertyAttribute>().ToArray();
            fieldAttributes.Add(fieldInfo, attributes);
        }
        return attributes;
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var propertyField = new PropertyField(property);
        var attributes = GetAttributes(fieldInfo);
        foreach (var item in attributes)
        {
            var drawer = GetDrawer(item.GetType());
            if(drawer != null)
            {
                drawer.OnCreatePropertyGUI(propertyField, property, item);
            }
        }
        return propertyField;
    }
}
