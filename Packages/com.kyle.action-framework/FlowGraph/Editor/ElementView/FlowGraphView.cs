using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
namespace Flow.EditorView
{
    public class FlowGraphView : GraphView
    {
        public FlowGraph Graph { get; set; }
        public FlowGraphView()
        {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return
                ports
                    .ToList()
                    .Where(endPort => endPort.direction != startPort.direction && endPort.node != startPort.node)
                    .ToList();
        }
    }
}