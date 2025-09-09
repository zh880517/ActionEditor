using UnityEngine;

namespace Flow
{
    public interface IFlowNode
    {
    }
    public interface IFlowInputable : IFlowNode
    {
    }

    public interface IFlowOutputable : IFlowNode
    {
    }

    public interface IFlowEntry : IFlowOutputable
    {
    }

    //Update节点
    public interface IFlowUpdateable : IFlowInputable, IFlowOutputable
    {
    }

    //条件节点，有两个输出，0是true，1是false
    public interface IFlowConditionable : IFlowOutputable
    {
    }

    //动态输出节点，可以动态添加输出端口
    //输出为索引，不符合的默认输出为-1；
    public interface IFlowDynamicOutputable : IFlowOutputable
    {
    }

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
    }
}
