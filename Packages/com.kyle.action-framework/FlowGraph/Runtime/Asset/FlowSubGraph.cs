using System.Collections.Generic;
using UnityEngine;

namespace Flow
{
    [System.Serializable]
    public struct SubGraphPort
    {
        public string Name;
        public string GUID;
    }

    public class FlowSubGraph : FlowGraph
    {
        public List<SubGraphPort> InputPorts = new List<SubGraphPort>();
        public List<SubGraphPort> OutputPorts = new List<SubGraphPort>();

        /// <summary>
        /// 子图输入节点引用，端口定义来自 InputPorts
        /// </summary>
        [HideInInspector]
        public SubGraphInputNode InputNode;
        /// <summary>
        /// 子图输出节点引用，端口定义来自 OutputPorts
        /// </summary>
        [HideInInspector]
        public SubGraphOutputNode OutputNode;

        /// <summary>
        /// 获取输入端口的内部EdgeID（从InputNode的PortEdges中查找）
        /// </summary>
        public ulong GetInputPortEdgeID(string portGUID)
        {
            return InputNode != null ? InputNode.GetPortEdgeID(portGUID) : 0;
        }

        /// <summary>
        /// 获取输出端口的内部EdgeID（从OutputNode的PortEdges中查找）
        /// </summary>
        public ulong GetOutputPortEdgeID(string portGUID)
        {
            return OutputNode != null ? OutputNode.GetPortEdgeID(portGUID) : 0;
        }

        public override bool CheckDelete(FlowNode node)
        {
            if (node is SubGraphInputNode || node is SubGraphOutputNode)
                return false;
            return base.CheckDelete(node);
        }
    }
}
