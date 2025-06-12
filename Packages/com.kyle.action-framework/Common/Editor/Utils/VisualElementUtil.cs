using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public static class VisualElementUtil
{
    public static void AlignParentLeft(this VisualElement element, float width)
    {
        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.top = 0;
        element.style.bottom = 0;
        element.style.width = width;
    }
    public static void AlignParentRight(this VisualElement element, float width)
    {
        element.style.position = Position.Absolute;
        element.style.right = 0;
        element.style.top = 0;
        element.style.bottom = 0;
        element.style.width = width;
    }

    public static void AlignParentTop(this VisualElement element, float height)
    {
        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.top = 0;
        element.style.right = 0;
        element.style.height = height;
    }

    public static void AlignParentBottom(this VisualElement element, float height)
    {
        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.bottom = 0;
        element.style.right = 0;
        element.style.height = height;
    }

    public static void ShowOutLine(this VisualElement element, Color color, float width)
    {
        element.style.borderLeftWidth = width;
        element.style.borderRightWidth = width;
        element.style.borderTopWidth = width;
        element.style.borderBottomWidth = width;
        element.style.borderLeftColor = color;
        element.style.borderRightColor = color;
        element.style.borderTopColor = color;
        element.style.borderBottomColor = color;
    }

    //设置鼠标进入VisualElement时的鼠标样式，如果有其它pickmode没有设置为ignore的VisualElement在上面，鼠标进入该区域无法切换光标样式，此时需要使用CursorRect
    public static void SetCursor(this VisualElement element, UnityEditor.MouseCursor cursor)
    {
        object objCursor = new UnityEngine.UIElements.Cursor();
        PropertyInfo fields = typeof(UnityEngine.UIElements.Cursor).GetProperty("defaultCursorId", BindingFlags.NonPublic | BindingFlags.Instance);
        fields.SetValue(objCursor, (int)cursor);
        element.style.cursor = new StyleCursor((UnityEngine.UIElements.Cursor)objCursor);
    }
}
