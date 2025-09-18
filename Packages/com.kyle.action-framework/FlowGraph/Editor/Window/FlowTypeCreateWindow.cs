using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flow.EditorView
{
    public struct FlowTypeCreateData
    {
        public Vector2 WorldPosition;
        public FlowNode OutputNode;
        public int OutputIndex;
        public FlowNode InputNode;
    }

    public class FlowTypeCreateWindow : FlowTypeSelectWindow
    {
        public FlowGraphView Current { get; set; }
        public FlowTypeCreateData Data;
        protected override IEnumerable<System.Type> GetTypes()
        {
            return types.Where(it => TypeMath(it));
        }

        private bool TypeMath(System.Type type)
        {
            if (Data.OutputNode != null)
            {
                var nodeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(type);
                if (nodeInfo == null || nodeInfo.DataType == null)
                    return false;
                return nodeInfo.HasInput;
            }
            else if (Data.InputNode != null)
            {
                var nodeInfo = FlowNodeTypeUtil.GetNodeTypeInfo(type);
                if (nodeInfo == null || nodeInfo.DataType == null)
                    return false;
                return nodeInfo.OutputType != NodeOutputType.None;
            }
            return true;
        }

        protected override void OnSelect(System.Type type, Vector2 localMousePosition)
        {
            Current?.OnNodeCreate(type, Data);
        }
    }
}
