using System.Collections.Generic;
using UnityEngine;
namespace Flow
{
    public class FlowGraph : CollectableScriptableObject
    {
        [Combined, Display("描述"), Multiline]
        public string Comment;
        [HideInInspector]
        public List<FlowNode> Nodes = new List<FlowNode>();
        [HideInInspector]
        public List<FlowEdge> Edges = new List<FlowEdge>();
        [HideInInspector]
        public List<FlowDataEdge> DataEdges = new List<FlowDataEdge>();
        [HideInInspector]
        public Vector3 Position;
        [HideInInspector]
        public Vector3 Scale = Vector3.one;
        [HideInInspector]
        [SerializeField]
        private uint keyIndex = 0;

        public ulong GenUID()
        {
            return FlowGUIDGenertor.GenID(keyIndex++);
        }

        public virtual bool CheckDelete(FlowNode node)
        {
            return true;
        }

        protected virtual void OnCreate() { }
    }
}