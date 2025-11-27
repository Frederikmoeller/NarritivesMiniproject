using DialogueSystem.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class DialogueEdgeConnectorListener : IEdgeConnectorListener
{
    private DialogueGraphView _graphView; // base class

    public DialogueEdgeConnectorListener(DialogueGraphView graphView)
    {
        _graphView = graphView;
        Debug.Log("DialogueEdgeConnectorListener created!");
    }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        Debug.Log($"OnDropOutsidePort called! Position: {position}");
        // Create a new node where the edge was dropped
        _graphView.CreateNodeFromEdgeDrop(edge, position);
    }

    public void OnDrop(GraphView graphView, Edge edge)
    {
        Debug.Log($"OnDrop called! Connecting {edge.output.node.name} to {edge.input.node.name}");
        _graphView.AddElement(edge);
        edge.userData = $"Edge from {edge.output.node.viewDataKey} to {edge.input.node.viewDataKey}";
        Debug.Log(edge.userData);

        // Update the node data connections
        _graphView.UpdateNodeConnections(edge);
    }
}