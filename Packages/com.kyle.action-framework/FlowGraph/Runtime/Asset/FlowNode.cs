using UnityEngine;

namespace Flow
{
    public interface IFlowOutputable
    {
    }

    public interface IFlowEntry : IFlowOutputable
    {
    }

    public interface IFlowInputable
    {
    }


    public class FlowNode : ScriptableObject
    {
        [HideInInspector]
        public FlowGraph Graph;
        [HideInInspector]
        public Rect Position;
        [HideInInspector]
        public bool Expanded = true;
    }
}
