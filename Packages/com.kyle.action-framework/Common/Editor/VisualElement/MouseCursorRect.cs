using UnityEditor;
using UnityEngine.UIElements;
public class MouseCursorRect : ImmediateModeElement
{
    public MouseCursor Cursor;
    protected override void ImmediateRepaint()
    {
        EditorGUIUtility.AddCursorRect(contentRect, Cursor);
    }
}