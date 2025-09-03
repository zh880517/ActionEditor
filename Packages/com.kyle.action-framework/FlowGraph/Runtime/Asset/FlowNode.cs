using System.Collections.Generic;
using UnityEngine;

namespace Flow
{
    public interface IFlowInputable
    {
    }

    public interface IFlowOutputable
    {
    }

    public interface IFlowEntry : IFlowOutputable
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

    public interface IFlowDynamicOutputable<T> : IFlowDynamicOutputable
    {
        List<T> Ports { get; }
    }


    public abstract class FlowNode : ScriptableObject
    {
        [HideInInspector]
        public FlowGraph Graph;
        [HideInInspector]
        public Rect Position;
        [HideInInspector]
        public bool Expanded = true;
    }
}
