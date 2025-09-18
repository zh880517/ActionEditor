using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

namespace Flow.EditorView
{
    internal class EdgeConnectorListener : IEdgeConnectorListener
    {
        private GraphViewChange m_GraphViewChange;
        private List<Edge> m_EdgesToCreate;
        private List<GraphElement> m_EdgesToDelete;

        public EdgeConnectorListener()
        {
            m_EdgesToCreate = new List<Edge>();
            m_EdgesToDelete = new List<GraphElement>();
            m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
        }

        //连接空白处的回调
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            //TODO:弹出节点创建窗口，在选择节点后创建节点并连接
            var graphView = edge.GetFirstOfType<FlowGraphView>();
            if (graphView != null)
            {
                var port = edge.input ?? edge.output;
                if(port is FlowPort flowPort)
                {
                    FlowTypeCreateData data = new FlowTypeCreateData 
                    {
                        WorldPosition = position,
                        OutputNode = flowPort.IsInput ? null : flowPort.Owner,
                        OutputIndex = flowPort.IsInput ? -1 : flowPort.Index,
                        InputNode = flowPort.IsInput ? flowPort.Owner : null
                    };

                    graphView.ShowNodeCreate(data);

                }
            }
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            m_EdgesToCreate.Clear();
            m_EdgesToCreate.Add(edge);
            m_EdgesToDelete.Clear();
            if (edge.input.capacity == Port.Capacity.Single)
            {
                foreach (Edge connection in edge.input.connections)
                {
                    if (connection != edge)
                        m_EdgesToDelete.Add(connection);
                }
            }
            if (edge.output.capacity == Port.Capacity.Single)
            {
                foreach (Edge connection in edge.output.connections)
                {
                    if (connection != edge)
                        m_EdgesToDelete.Add(connection);
                }
            }
            if (m_EdgesToDelete.Count > 0)
                graphView.DeleteElements(m_EdgesToDelete);
            List<Edge> edgesToCreate = m_EdgesToCreate;
            if (graphView.graphViewChanged != null)
                edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
            foreach (Edge newEdge in edgesToCreate)
            {
                graphView.AddElement(newEdge);
                edge.input.Connect(newEdge);
                edge.output.Connect(newEdge);
            }
        }
    }
}
