using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class DialogueEdgeConnectorListener : IEdgeConnectorListener
{
    private GraphView _graphView; // base class

    public DialogueEdgeConnectorListener(GraphView graphView)
    {
        _graphView = graphView;
    }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        // Optional: handle dropping edge outside a port
    }

    public void OnDrop(GraphView graphView, Edge edge)
    {
        _graphView.AddElement(edge);
        edge.userData = $"Edge from {edge.output.node.viewDataKey} to {edge.input.node.viewDataKey}";
        Debug.Log(edge.userData);
    }
}