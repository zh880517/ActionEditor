using System.Collections.Generic;
using UnityEngine;

namespace Flow.EditorView
{
    public enum SelectionType
    {
        Node,
        Edge,
        DataEdge,
    }

    [System.Serializable]
    public struct SelectionData
    {
        public SelectionType Type;
        public FlowNode Node;
        public FlowEdge Edge;
        public ulong EdgeID;
    }

    public class FlowGraphEditorData : ScriptableObject
    {
        public List<SelectionData> Selections = new List<SelectionData>();
    }
}
