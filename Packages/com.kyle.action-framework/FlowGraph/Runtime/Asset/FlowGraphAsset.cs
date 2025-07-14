using System.Collections.Generic;
using UnityEngine;
namespace Flow
{
    public class FlowGraphAsset : ScriptableObject
    {
        [Combined, Display("√Ë ˆ"), Multiline]
        public string Comment;
        [HideInInspector]
        public List<FlowNode> Nodes = new List<FlowNode>();
        [HideInInspector]
        public List<FlowEdge> Edges = new List<FlowEdge>();
        [HideInInspector]
        public List<FlowDataEdge> DataEdges = new List<FlowDataEdge>();
    }
}