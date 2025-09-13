using UnityEngine;

namespace Flow
{
    public abstract class FlowNode : ScriptableObject
    {
        [HideInInspector]
        public FlowGraph Graph;
        [HideInInspector]
        public Rect Position;
        [HideInInspector]
        public bool Expanded = true;
        public abstract bool IsDefine<T>();
        public virtual void OnCreate() { }

        public abstract FlowNodeRuntimeData Export();
    }
}
