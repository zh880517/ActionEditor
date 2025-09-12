using UnityEngine;

namespace Flow.EditorView
{
    public interface IFlowEditorWindow
    {
        Vector2 ScreenPositionToWorldPosition(Vector2 screenPosition);
        Vector2 WorldPositionToScreenPosition(Vector2 worldPosition);
    }
}
