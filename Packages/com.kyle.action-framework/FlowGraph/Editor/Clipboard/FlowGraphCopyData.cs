using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Flow.EditorView
{
    [System.Serializable]
    public class FlowNodeCopy
    {
        public string GUID;
        public MonoScript NodeScript;
        public Rect Position;
        public string JsonData;
    }
    [System.Serializable]
    public class FlowEdgeCopy
    {
        public string OutputNodeGUID;
        public int OutputPortIndex;
        public string InputNodeGUID;
    }
    [System.Serializable]
    public class FlowDataEdgeCopy
    {
        public string OutputNodeGUID;
        public string OutputPortName;
        public string InputNodeGUID;
        public string InputPortName;
    }


    [System.Serializable]
    public class FlowGraphCopyData
    {
        public MonoScript GraphScript;
        public List<FlowNodeCopy> Nodes = new List<FlowNodeCopy>();
        public List<FlowEdgeCopy> Edges = new List<FlowEdgeCopy>();
        public List<FlowDataEdgeCopy> DataEdges = new List<FlowDataEdgeCopy>();
    }
}
